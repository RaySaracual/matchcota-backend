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

        // Render provides postgres:// URI; convert it and ensure SSL is enabled for production.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
        {
            dataSourceBuilder.ConnectionStringBuilder.SslMode = SslMode.Require;
            dataSourceBuilder.ConnectionStringBuilder.TrustServerCertificate = true;
        }

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<MatchcotaDbContext>(options =>
            options.UseNpgsql(dataSource));

        return services;
    }
}
