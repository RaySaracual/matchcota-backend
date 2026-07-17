namespace Matchcota.Services.Discovery.Contracts;

public sealed record DiscoveryCandidate(
    Guid DogId,
    string Name,
    string Breed,
    int? AgeMonths,
    string Bio,
    string? PhotoUrl,
    double DistanceKm);
