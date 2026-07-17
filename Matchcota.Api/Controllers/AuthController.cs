using System.Security.Claims;
using Matchcota.Api.Auth;
using Matchcota.Api.Contracts.Auth;
using Matchcota.Services.Auth;
using Matchcota.Services.Auth.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matchcota.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, IJwtTokenGenerator jwtTokenGenerator) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] AuthRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest("DisplayName is required for registration.");
        }

        var authResult = await _authService.RegisterAsync(
            new RegisterRequest(request.Email, request.Password, request.DisplayName),
            cancellationToken);

        if (authResult is null)
        {
            return Conflict("Email is already registered.");
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(authResult);
        var refreshToken = await _authService.IssueRefreshTokenAsync(authResult.UserId, cancellationToken);

        return Ok(new AuthResponseDto(
            authResult.UserId,
            authResult.Email,
            authResult.DisplayName,
            accessToken,
            refreshToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] AuthRequestDto request, CancellationToken cancellationToken)
    {
        var authResult = await _authService.LoginAsync(new LoginRequest(request.Email, request.Password), cancellationToken);

        if (authResult is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(authResult);
        var refreshToken = await _authService.IssueRefreshTokenAsync(authResult.UserId, cancellationToken);

        return Ok(new AuthResponseDto(
            authResult.UserId,
            authResult.Email,
            authResult.DisplayName,
            accessToken,
            refreshToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("RefreshToken is required.");
        }

        var refreshResult = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (refreshResult is null)
        {
            return Unauthorized("Refresh token is invalid or revoked.");
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(refreshResult.AuthResult);

        return Ok(new AuthResponseDto(
            refreshResult.AuthResult.UserId,
            refreshResult.AuthResult.Email,
            refreshResult.AuthResult.DisplayName,
            accessToken,
            refreshResult.RefreshToken));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("RefreshToken is required.");
        }

        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized("Invalid token subject.");
        }

        await _authService.RevokeAllRefreshTokensAsync(userId, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        return Ok(new
        {
            UserId = userId,
            Email = email,
            DisplayName = User.Identity?.Name ?? User.FindFirstValue("name")
        });
    }
}
