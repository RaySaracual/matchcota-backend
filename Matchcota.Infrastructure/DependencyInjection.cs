using Matchcota.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Matchcota.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        // Render provides the connection string as a postgres:// URI.
        // NpgsqlConnectionStringBuilder only accepts key=value format, so convert it first.
        if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
        {
            connectionString = ConvertPostgresUriToConnectionString(connectionString);
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<MatchcotaDbContext>(options =>
            options.UseNpgsql(dataSource));

        return services;
    }

    private static string ConvertPostgresUriToConnectionString(string uri)
    {
        var parsed = new Uri(uri);
        var userInfo = parsed.UserInfo.Split(':', 2);
        var host = parsed.Host;
        var port = parsed.Port > 0 ? parsed.Port : 5432;
        var database = parsed.AbsolutePath.TrimStart('/');
        var username = Uri.UnescapeDataString(userInfo.Length > 0 ? userInfo[0] : string.Empty);
        var password = Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : string.Empty);

        // SslMode=Require is needed for Render's managed PostgreSQL external connections.
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;Trust Server Certificate=true";
    }
}
