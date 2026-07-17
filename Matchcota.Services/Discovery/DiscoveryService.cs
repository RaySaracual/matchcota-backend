using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Discovery.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Services.Discovery;

public sealed class DiscoveryService(MatchcotaDbContext dbContext) : IDiscoveryService
{
    private readonly MatchcotaDbContext _dbContext = dbContext;

    public async Task<bool> DogBelongsToUserAsync(
        Guid dogId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Dogs
            .AsNoTracking()
            .AnyAsync(d => d.Id == dogId && d.OwnerId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<DiscoveryCandidate>> GetCandidatesAsync(
        Guid sourceDogId,
        Guid requestingUserId,
        int page,
        int pageSize,
        double radiusKm,
        CancellationToken cancellationToken)
    {
        var sourceDog = await _dbContext.Dogs
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == sourceDogId && d.OwnerId == requestingUserId && d.IsActive, cancellationToken);

        if (sourceDog?.Latitude is null || sourceDog.Longitude is null)
        {
            return Array.Empty<DiscoveryCandidate>();
        }

        var srcLat = sourceDog.Latitude.Value;
        var srcLon = sourceDog.Longitude.Value;
        var skip = (page - 1) * pageSize;

        // Pull all active candidates except self and already-swiped, then filter by distance in memory.
        // For MVP scale this is acceptable; replace with PostGIS when available in production.
        var pool = await _dbContext.Dogs
            .AsNoTracking()
            .Where(d =>
                d.IsActive &&
                d.Id != sourceDogId &&
                d.Latitude != null &&
                d.Longitude != null &&
                !_dbContext.Swipes.Any(s => s.SourceDogId == sourceDogId && s.TargetDogId == d.Id))
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Breed,
                d.BirthDate,
                d.Bio,
                d.Latitude,
                d.Longitude,
                PhotoUrl = d.Media
                    .OrderBy(m => m.CreatedAtUtc)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return pool
            .Select(c => new
            {
                c.Id, c.Name, c.Breed, c.BirthDate, c.Bio, c.PhotoUrl,
                DistanceKm = HaversineKm(srcLat, srcLon, c.Latitude!.Value, c.Longitude!.Value),
            })
            .Where(c => c.DistanceKm <= radiusKm)
            .OrderBy(c => c.DistanceKm)
            .Skip(skip)
            .Take(pageSize)
            .Select(c => new DiscoveryCandidate(
                DogId: c.Id,
                Name: c.Name,
                Breed: c.Breed,
                AgeMonths: c.BirthDate.HasValue ? CalculateAgeMonths(c.BirthDate.Value) : null,
                Bio: c.Bio,
                PhotoUrl: c.PhotoUrl,
                DistanceKm: Math.Round(c.DistanceKm, 1)))
            .ToList();
    }

    public async Task<IReadOnlyList<DiscoveryMatch>> GetMatchesAsync(
        Guid dogId,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var dogOwned = await DogBelongsToUserAsync(dogId, requestingUserId, cancellationToken);
        if (!dogOwned)
        {
            return Array.Empty<DiscoveryMatch>();
        }

        var matches = await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.IsActive && (m.DogAId == dogId || m.DogBId == dogId))
            .Include(m => m.DogA).ThenInclude(d => d.Media)
            .Include(m => m.DogB).ThenInclude(d => d.Media)
            .OrderByDescending(m => m.MatchedAtUtc)
            .ToListAsync(cancellationToken);

        return matches
            .Select(m =>
            {
                var other = m.DogAId == dogId ? m.DogB : m.DogA;
                var photo = other.Media
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => x.MediaUrl)
                    .FirstOrDefault();
                return new DiscoveryMatch(m.Id, other.Id, other.Name, photo, m.MatchedAtUtc);
            })
            .ToList();
    }

    private static int CalculateAgeMonths(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var months = (today.Year - birthDate.Year) * 12 + (today.Month - birthDate.Month);
        return Math.Max(0, months);
    }

    /// <summary>Haversine distance in km between two WGS-84 coordinates.</summary>
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
