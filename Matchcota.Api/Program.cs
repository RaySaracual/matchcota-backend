using System.Text;
using Matchcota.Api.Auth;
using Matchcota.Api.Health;
using Matchcota.Api.Hubs;
using Matchcota.Api.Middleware;
using Matchcota.Api.Storage;
using Matchcota.Infrastructure;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Dogs;
using Matchcota.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using System.Net.Mime;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCors";

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter())
        .WriteTo.File(
            new RenderedCompactJsonFormatter(),
            "logs/matchcota-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14);
});

// Add services to the container.

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are missing.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? [];

    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run pending migrations on startup (with retry for cloud environments where DB may not be ready)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchcotaDbContext>();
            await db.Database.MigrateAsync();
            await EnsureRefreshTokensTableAsync(db, logger);
            logger.LogInformation("Database migrations applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogError(ex, "Failed to apply migrations. Retries left: {Retries}", retries);
            if (retries == 0) throw;
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (ShouldUseHttpsRedirection(app.Configuration))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms requestId={RequestId}";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
    };
});

app.UseStaticFiles();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                error = e.Value.Exception?.Message,
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();

static bool ShouldUseHttpsRedirection(IConfiguration configuration)
{
    var urls = configuration["ASPNETCORE_URLS"];
    if (string.IsNullOrWhiteSpace(urls))
    {
        return true;
    }

    return urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
}

static async Task EnsureRefreshTokensTableAsync(MatchcotaDbContext db, Microsoft.Extensions.Logging.ILogger logger)
{
    const string sql = """
    CREATE TABLE IF NOT EXISTS "RefreshTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(200) NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
        "RevokedAtUtc" timestamp with time zone NULL,
        "ReplacedByTokenHash" character varying(200) NULL,
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id")
    );

    CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshTokens_TokenHash"
        ON "RefreshTokens" ("TokenHash");

    CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId_RevokedAtUtc"
        ON "RefreshTokens" ("UserId", "RevokedAtUtc");

    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN ('Users', 'users')
        ) THEN
            IF NOT EXISTS (
                SELECT 1
                FROM pg_constraint
                WHERE conname = 'FK_RefreshTokens_Users_UserId'
            ) THEN
                ALTER TABLE "RefreshTokens"
                    ADD CONSTRAINT "FK_RefreshTokens_Users_UserId"
                    FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
            END IF;
        END IF;
    END
    $$;
    """;

    await db.Database.ExecuteSqlRawAsync(sql);
    logger.LogInformation("Verified RefreshTokens table and indexes.");
}
