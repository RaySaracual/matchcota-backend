using System.Security.Claims;
using Matchcota.Api.Contracts.Dogs;
using Matchcota.Services.Dogs;
using Matchcota.Services.Dogs.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Api.Controllers;

[ApiController]
[Route("api/v1/dogs")]
[Authorize]
public sealed class DogsController(IDogsService dogsService) : ControllerBase
{
    private readonly IDogsService _dogsService = dogsService;

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<DogSummaryDto>>> GetMyDogs(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var dogs = await _dogsService.GetMyDogsAsync(userId, cancellationToken);
        return Ok(dogs.Select(d => new DogSummaryDto(d.DogId, d.Name, d.Breed, d.PhotoUrl)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<DogSummaryDto>> CreateDog(
        [FromBody] CreateDogRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var request = new CreateDogRequest(
            dto.Name,
            dto.Breed,
            dto.Bio ?? string.Empty,
            dto.BirthDate,
            dto.Latitude,
            dto.Longitude);

        var dog = await _dogsService.CreateDogAsync(userId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetMyDogs),
            new DogSummaryDto(dog.DogId, dog.Name, dog.Breed, dog.PhotoUrl));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
