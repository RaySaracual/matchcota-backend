namespace Matchcota.Api.Contracts.Chat;

public sealed record ChatMessagesPageDto(
    IReadOnlyList<ChatMessageDto> Items,
    int Page,
    int PageSize,
    bool HasMore);
