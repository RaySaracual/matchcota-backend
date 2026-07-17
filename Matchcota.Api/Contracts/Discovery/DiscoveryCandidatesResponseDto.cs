namespace Matchcota.Api.Contracts.Discovery;

public sealed record DiscoveryCandidatesResponseDto(
    IReadOnlyList<CandidateDto> Items,
    int Page,
    int PageSize,
    bool HasMore);
