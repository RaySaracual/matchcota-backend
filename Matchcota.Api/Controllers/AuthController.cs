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

        var token = _jwtTokenGenerator.GenerateToken(authResult);
        return Ok(new AuthResponseDto(authResult.UserId, authResult.Email, authResult.DisplayName, token));
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

        var token = _jwtTokenGenerator.GenerateToken(authResult);
        return Ok(new AuthResponseDto(authResult.UserId, authResult.Email, authResult.DisplayName, token));
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
