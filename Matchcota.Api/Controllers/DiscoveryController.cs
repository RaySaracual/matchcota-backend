using System.Security.Claims;
using Matchcota.Api.Contracts.Discovery;
using Matchcota.Services.Discovery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Api.Controllers;

[ApiController]
[Route("api/v1/discovery")]
[Authorize]
public sealed class DiscoveryController(
    IDiscoveryService discoveryService,
    ISwipeService swipeService) : ControllerBase
{
    private readonly IDiscoveryService _discoveryService = discoveryService;
    private readonly ISwipeService _swipeService = swipeService;

    /// <summary>
    /// Returns nearby dog candidates for discovery.
    /// Distance is relative — exact coordinates of other dogs are never exposed.
    /// </summary>
    [HttpGet("candidates")]
    public async Task<ActionResult<DiscoveryCandidatesResponseDto>> GetCandidates(
        [FromQuery] Guid sourceDogId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] double radiusKm = 50.0,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 50) pageSize = 10;
        if (radiusKm is <= 0 or > 200) radiusKm = 50.0;

        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var candidates = await _discoveryService.GetCandidatesAsync(
            sourceDogId, userId, page, pageSize, radiusKm, cancellationToken);

        var items = candidates
            .Select(c => new CandidateDto(c.DogId, c.Name, c.Breed, c.AgeMonths, c.Bio, c.PhotoUrl, c.DistanceKm))
            .ToList();

        return Ok(new DiscoveryCandidatesResponseDto(items, page, pageSize, items.Count == pageSize));
    }

    /// <summary>
    /// Records a swipe (like or dislike). Creates a Match automatically on mutual like.
    /// </summary>
    [HttpPost("swipes")]
    public async Task<ActionResult<SwipeResponseDto>> RecordSwipe(
        [FromBody] SwipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (request.SourceDogId == request.TargetDogId)
        {
            return BadRequest("Source and target dog must be different.");
        }

        try
        {
            var result = await _swipeService.RecordSwipeAsync(
                request.SourceDogId,
                request.TargetDogId,
                request.IsLike,
                userId,
                cancellationToken);

            return Ok(new SwipeResponseDto(result.SwipeId, result.MatchCreated, result.MatchId));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Returns active matches for the given dog.
    /// </summary>
    [HttpGet("matches")]
    public async Task<ActionResult<IReadOnlyList<MatchDto>>> GetMatches(
        [FromQuery] Guid dogId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var matches = await _discoveryService.GetMatchesAsync(dogId, userId, cancellationToken);
        var dtos = matches
            .Select(m => new MatchDto(m.MatchId, m.OtherDogId, m.OtherDogName, m.OtherDogPhotoUrl, m.MatchedAtUtc))
            .ToList();

        return Ok(dtos);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
