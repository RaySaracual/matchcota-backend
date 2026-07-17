namespace Matchcota.Services.Chat.Contracts;

public sealed record PagedMessagesResult(
    IReadOnlyList<ChatMessageResult> Items,
    int Page,
    int PageSize,
    bool HasMore);
