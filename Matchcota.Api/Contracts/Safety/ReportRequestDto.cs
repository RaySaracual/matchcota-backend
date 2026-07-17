namespace Matchcota.Api.Contracts.Safety;

public sealed record ReportRequestDto(Guid ReportedDogId, string Category, string? Detail);
