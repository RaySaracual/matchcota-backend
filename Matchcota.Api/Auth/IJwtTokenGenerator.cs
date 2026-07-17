using Matchcota.Services.Auth.Contracts;

namespace Matchcota.Api.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(AuthResult authResult);
}
