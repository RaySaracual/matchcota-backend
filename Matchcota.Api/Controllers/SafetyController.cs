using System.Security.Claims;
using Matchcota.Api.Contracts.Safety;
using Matchcota.Services.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Api.Controllers;

[ApiController]
[Route("api/v1/safety")]
[Authorize]
public sealed class SafetyController(ISafetyService safetyService) : ControllerBase
{
    private readonly ISafetyService _safetyService = safetyService;

    [HttpPost("report")]
    public async Task<IActionResult> Report(
        [FromBody] ReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            await _safetyService.ReportAsync(
                userId,
                request.ReportedDogId,
                request.Category,
                request.Detail,
                cancellationToken);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("block/{dogId:guid}")]
    public async Task<IActionResult> Block(
        Guid dogId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            await _safetyService.BlockAsync(userId, dogId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("block/{dogId:guid}")]
    public async Task<IActionResult> Unblock(
        Guid dogId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var removed = await _safetyService.UnblockAsync(userId, dogId, cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
