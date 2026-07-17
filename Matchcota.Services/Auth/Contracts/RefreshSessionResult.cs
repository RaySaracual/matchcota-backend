namespace Matchcota.Services.Auth.Contracts;

public sealed record RefreshSessionResult(AuthResult AuthResult, string RefreshToken);