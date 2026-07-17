namespace Matchcota.Api.Contracts.Dogs;

public sealed record DogSummaryDto(
    Guid DogId,
    string Name,
    string Breed,
    string? PhotoUrl,
    string Bio = "",
    bool IsActive = true,
    double? Latitude = null,
    double? Longitude = null);

