namespace Matchcota.Api.Contracts.Dogs;

public sealed record DogSummaryDto(
    Guid DogId,
    string Name,
    string Breed,
    string? PhotoUrl);
