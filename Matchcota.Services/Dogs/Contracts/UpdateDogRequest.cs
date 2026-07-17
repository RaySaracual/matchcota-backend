namespace Matchcota.Services.Dogs.Contracts;

public sealed record UpdateDogRequest(
    string Name,
    string Breed,
    string Bio,
    DateOnly? BirthDate,
    double? Latitude,
    double? Longitude);
