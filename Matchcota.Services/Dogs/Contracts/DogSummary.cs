namespace Matchcota.Services.Dogs.Contracts;

public sealed record DogSummary(
    Guid DogId,
    string Name,
    string Breed,
    string? PhotoUrl,
    string Bio = "",
    bool IsActive = true,
    double? Latitude = null,
    double? Longitude = null);

