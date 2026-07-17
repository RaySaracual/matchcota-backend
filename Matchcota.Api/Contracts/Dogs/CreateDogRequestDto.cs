using System.ComponentModel.DataAnnotations;

namespace Matchcota.Api.Contracts.Dogs;

public sealed record CreateDogRequestDto(
    [Required][MaxLength(120)] string Name,
    [Required][MaxLength(120)] string Breed,
    [MaxLength(500)] string? Bio,
    DateOnly? BirthDate,
    double? Latitude,
    double? Longitude);
