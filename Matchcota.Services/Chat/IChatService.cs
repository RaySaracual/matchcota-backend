using Matchcota.Services.Chat.Contracts;

namespace Matchcota.Services.Chat;

public interface IChatService
{
    Task EnsureUserBelongsToMatchAsync(Guid userId, Guid matchId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationResult>> GetConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<Guid>? blockedDogIds = null);

    Task MarkAsReadAsync(
        Guid userId,
        Guid matchId,
        CancellationToken cancellationToken);

    Task<PagedMessagesResult> GetMessagesAsync(
        Guid userId,
        Guid matchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ChatMessageResult> SendMessageAsync(
        Guid userId,
        Guid matchId,
        Guid senderDogId,
        string content,
        CancellationToken cancellationToken);
}
