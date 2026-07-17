namespace Matchcota.Services.Auth.Contracts;

public sealed record AuthResult(Guid UserId, string Email, string DisplayName);
