using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Tests.Auth;

public sealed class AuthServiceRefreshTests : IDisposable
{
    private readonly MatchcotaDbContext _dbContext;
    private readonly AuthService _authService;
    private readonly Guid _userId = Guid.NewGuid();

    public AuthServiceRefreshTests()
    {
        var options = new DbContextOptionsBuilder<MatchcotaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new MatchcotaDbContext(options);
        _authService = new AuthService(_dbContext);

        _dbContext.Users.Add(new User
        {
            Id = _userId,
            Email = "auth-refresh@matchcota.test",
            DisplayName = "Auth Refresh",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123!"),
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokenAndRevokesPrevious()
    {
        var issuedToken = await _authService.IssueRefreshTokenAsync(_userId, CancellationToken.None);

        var refreshResult = await _authService.RefreshAsync(issuedToken, CancellationToken.None);

        Assert.NotNull(refreshResult);
        Assert.Equal(_userId, refreshResult!.AuthResult.UserId);
        Assert.NotEqual(issuedToken, refreshResult.RefreshToken);

        var tokens = await _dbContext.RefreshTokens
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(tokens[0].ReplacedByTokenHash));
        Assert.Null(tokens[1].RevokedAtUtc);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNullForRevokedToken()
    {
        var issuedToken = await _authService.IssueRefreshTokenAsync(_userId, CancellationToken.None);

        var firstRefresh = await _authService.RefreshAsync(issuedToken, CancellationToken.None);
        Assert.NotNull(firstRefresh);

        var secondRefreshUsingSameToken = await _authService.RefreshAsync(issuedToken, CancellationToken.None);

        Assert.Null(secondRefreshUsingSameToken);
    }

    [Fact]
    public async Task RevokeAllRefreshTokensAsync_RevokesEveryActiveTokenForUser()
    {
        await _authService.IssueRefreshTokenAsync(_userId, CancellationToken.None);
        await _authService.IssueRefreshTokenAsync(_userId, CancellationToken.None);

        var revokedCount = await _authService.RevokeAllRefreshTokensAsync(_userId, CancellationToken.None);

        Assert.Equal(2, revokedCount);
        Assert.All(_dbContext.RefreshTokens, token => Assert.NotNull(token.RevokedAtUtc));
    }
}
