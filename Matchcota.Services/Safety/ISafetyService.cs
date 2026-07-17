namespace Matchcota.Services.Safety;

public interface ISafetyService
{
    Task ReportAsync(
        Guid reportedByUserId,
        Guid reportedDogId,
        string category,
        string? detail,
        CancellationToken cancellationToken);

    Task BlockAsync(
        Guid blockerUserId,
        Guid blockedDogId,
        CancellationToken cancellationToken);

    Task<bool> UnblockAsync(
        Guid blockerUserId,
        Guid blockedDogId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetBlockedDogIdsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
