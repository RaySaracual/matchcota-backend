using Matchcota.Services.Auth.Contracts;

namespace Matchcota.Services.Auth;

public interface IAuthService
{
    Task<AuthResult?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
