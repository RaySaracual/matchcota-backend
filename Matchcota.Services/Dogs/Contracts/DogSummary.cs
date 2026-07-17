namespace Matchcota.Services.Dogs.Contracts;

public sealed record DogSummary(Guid DogId, string Name, string Breed, string? PhotoUrl);
