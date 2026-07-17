namespace Matchcota.Services.Chat.Contracts;

public sealed record ChatMessageResult(
    Guid MessageId,
    Guid MatchId,
    Guid SenderDogId,
    string Content,
    DateTime SentAtUtc);
