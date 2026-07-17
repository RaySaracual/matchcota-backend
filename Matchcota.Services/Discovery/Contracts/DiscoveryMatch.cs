namespace Matchcota.Services.Discovery.Contracts;

public sealed record DiscoveryMatch(
    Guid MatchId,
    Guid OtherDogId,
    string OtherDogName,
    string? OtherDogPhotoUrl,
    DateTime MatchedAtUtc);
