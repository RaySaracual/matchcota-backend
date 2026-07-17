namespace Matchcota.Api.Contracts.Chat;

public sealed record ChatMessageDto(
    Guid MessageId,
    Guid MatchId,
    Guid SenderDogId,
    string Content,
    DateTime SentAtUtc);
