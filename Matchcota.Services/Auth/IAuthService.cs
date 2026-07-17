using Matchcota.Services.Auth.Contracts;

namespace Matchcota.Services.Auth;

public interface IAuthService
{
    Task<AuthResult?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<RefreshSessionResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<int> RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken);
}
