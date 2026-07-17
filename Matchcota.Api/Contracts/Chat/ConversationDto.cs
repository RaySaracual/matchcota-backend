namespace Matchcota.Api.Contracts.Chat;

public sealed record ConversationDto(
    Guid MatchId,
    Guid OtherDogId,
    string OtherDogName,
    string OtherDogBreed,
    string? OtherDogPhotoUrl,
    string? LastMessageContent,
    DateTime? LastMessageSentAtUtc,
    Guid MyDogId,
    int UnreadCount);
