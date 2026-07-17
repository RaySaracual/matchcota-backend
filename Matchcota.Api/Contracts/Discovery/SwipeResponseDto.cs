namespace Matchcota.Api.Contracts.Discovery;

public sealed record SwipeResponseDto(Guid SwipeId, bool MatchCreated, Guid? MatchId);
