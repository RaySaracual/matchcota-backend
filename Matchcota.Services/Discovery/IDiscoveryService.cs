using Matchcota.Services.Discovery.Contracts;

namespace Matchcota.Services.Discovery;

public interface IDiscoveryService
{
    Task<IReadOnlyList<DiscoveryCandidate>> GetCandidatesAsync(
        Guid sourceDogId,
        Guid requestingUserId,
        int page,
        int pageSize,
        double radiusKm,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DiscoveryMatch>> GetMatchesAsync(
        Guid dogId,
        Guid requestingUserId,
        CancellationToken cancellationToken);

    Task<bool> DogBelongsToUserAsync(
        Guid dogId,
        Guid userId,
        CancellationToken cancellationToken);
}
