using Matchcota.Services.Discovery.Contracts;

namespace Matchcota.Services.Discovery;

public interface ISwipeService
{
    Task<SwipeResult> RecordSwipeAsync(
        Guid sourceDogId,
        Guid targetDogId,
        bool isLike,
        Guid requestingUserId,
        CancellationToken cancellationToken);
}
