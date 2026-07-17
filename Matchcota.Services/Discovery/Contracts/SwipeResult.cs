namespace Matchcota.Services.Discovery.Contracts;

public sealed record SwipeResult(Guid SwipeId, bool MatchCreated, Guid? MatchId);
