namespace Matchcota.Services.Dogs.Contracts;

public sealed record CreateDogRequest(
    string Name,
    string Breed,
    string Bio,
    DateOnly? BirthDate,
    double? Latitude,
    double? Longitude);
