namespace Matchcota.Api.Contracts.Discovery;

public sealed record CandidateDto(
    Guid DogId,
    string Name,
    string Breed,
    int? AgeMonths,
    string Bio,
    string? PhotoUrl,
    double DistanceKm);
