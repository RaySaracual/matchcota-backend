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
        CancellationToken cancellationToken,
        IReadOnlyCollection<Guid>? blockedDogIds = null);

    Task<IReadOnlyList<DiscoveryMatch>> GetMatchesAsync(
        Guid dogId,
        Guid requestingUserId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<Guid>? blockedDogIds = null);

    Task<bool> DogBelongsToUserAsync(
        Guid dogId,
        Guid userId,
        CancellationToken cancellationToken);
}
