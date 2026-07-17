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
public sealed class DogsController(
    IDogsService dogsService,
    IStorageService storageService) : ControllerBase
{
    private readonly IDogsService _dogsService = dogsService;
    private readonly IStorageService _storageService = storageService;
    private const long MaxUploadSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/jpg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<DogSummaryDto>>> GetMyDogs(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var dogs = await _dogsService.GetMyDogsAsync(userId, cancellationToken);
        return Ok(dogs.Select(d => new DogSummaryDto(d.DogId, d.Name, d.Breed, d.PhotoUrl, d.Bio, d.IsActive, d.Latitude, d.Longitude)).ToList());
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
            new DogSummaryDto(dog.DogId, dog.Name, dog.Breed, dog.PhotoUrl, dog.Bio, dog.IsActive, dog.Latitude, dog.Longitude));
    }

    [HttpPut("{dogId:guid}")]
    public async Task<ActionResult<DogSummaryDto>> UpdateDog(
        Guid dogId,
        [FromBody] UpdateDogRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var request = new UpdateDogRequest(
            dto.Name,
            dto.Breed,
            dto.Bio ?? string.Empty,
            dto.BirthDate,
            dto.Latitude,
            dto.Longitude);

        var dog = await _dogsService.UpdateDogAsync(userId, dogId, request, cancellationToken);
        return Ok(new DogSummaryDto(dog.DogId, dog.Name, dog.Breed, dog.PhotoUrl, dog.Bio, dog.IsActive, dog.Latitude, dog.Longitude));
    }

    [HttpPatch("{dogId:guid}/status")]
    public async Task<IActionResult> SetDogStatus(
        Guid dogId,
        [FromBody] PatchDogStatusRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _dogsService.SetDogStatusAsync(userId, dogId, dto.IsActive, cancellationToken);
        return NoContent();
    }

    [HttpPost("{dogId:guid}/media")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<UploadDogMediaResponseDto>> UploadMedia(
        Guid dogId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var belongsToUser = await _dogsService.DogBelongsToUserAsync(dogId, userId, cancellationToken);
        if (!belongsToUser)
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("A media file is required.");
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            return BadRequest("File size exceeds 5 MB.");
        }

        if (!AllowedTypes.TryGetValue(file.ContentType, out var extension))
        {
            return BadRequest("Unsupported media type. Allowed: jpg, png, webp.");
        }

        await using var stream = file.OpenReadStream();
        var relativePath = await _storageService.SaveAsync(stream, extension, cancellationToken);
        var publicUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";

        await _dogsService.ReplacePrimaryMediaAsync(dogId, publicUrl, file.ContentType, cancellationToken);
        return Ok(new UploadDogMediaResponseDto(publicUrl));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
