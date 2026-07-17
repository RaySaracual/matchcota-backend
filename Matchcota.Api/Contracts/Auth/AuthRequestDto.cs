namespace Matchcota.Api.Contracts.Auth;

public sealed record AuthRequestDto(string Email, string Password, string? DisplayName);
