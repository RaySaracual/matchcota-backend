namespace Matchcota.Services.Auth.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
