namespace Matchcota.Api.Contracts.Auth;

public sealed record AuthResponseDto(Guid UserId, string Email, string DisplayName, string AccessToken);
