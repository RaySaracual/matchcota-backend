namespace Matchcota.Services.Chat.Contracts;

public sealed record ConversationResult(
    Guid MatchId,
    Guid OtherDogId,
    string OtherDogName,
    string OtherDogBreed,
    string? OtherDogPhotoUrl,
    string? LastMessageContent,
    DateTime? LastMessageSentAtUtc,
    Guid MyDogId,
    int UnreadCount);
