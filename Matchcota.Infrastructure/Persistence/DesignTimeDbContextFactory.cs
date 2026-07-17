using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Matchcota.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MatchcotaDbContext>
{
    public MatchcotaDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MATCHCOTA_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=matchcota;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<MatchcotaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MatchcotaDbContext(optionsBuilder.Options);
    }
}
