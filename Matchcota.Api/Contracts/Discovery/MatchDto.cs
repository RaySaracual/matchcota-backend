namespace Matchcota.Api.Contracts.Discovery;

public sealed record MatchDto(
    Guid MatchId,
    Guid OtherDogId,
    string OtherDogName,
    string? OtherDogPhotoUrl,
    DateTime MatchedAtUtc);
