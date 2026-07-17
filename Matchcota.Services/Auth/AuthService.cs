using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Auth.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Services.Auth;

public sealed class AuthService(MatchcotaDbContext dbContext) : IAuthService
{
    private readonly MatchcotaDbContext _dbContext = dbContext;

    public async Task<AuthResult?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(user.Id, user.Email, user.DisplayName);
    }

    public async Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            return null;
        }

        return new AuthResult(user.Id, user.Email, user.DisplayName);
    }
}
