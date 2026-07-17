using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Discovery;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Tests.Discovery;

public sealed class SwipeServiceTests : IDisposable
{
    private readonly MatchcotaDbContext _dbContext;
    private readonly SwipeService _swipeService;

    // Test users and dogs
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private Guid _dogA;
    private Guid _dogB;

    public SwipeServiceTests()
    {
        var options = new DbContextOptionsBuilder<MatchcotaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new MatchcotaDbContext(options);
        _swipeService = new SwipeService(_dbContext);

        SeedData();
    }

    private void SeedData()
    {
        _dogA = Guid.NewGuid();
        _dogB = Guid.NewGuid();

        _dbContext.Users.AddRange(
            new User { Id = _userA, Email = "a@test.com", DisplayName = "A", PasswordHash = "x" },
            new User { Id = _userB, Email = "b@test.com", DisplayName = "B", PasswordHash = "x" });

        _dbContext.Dogs.AddRange(
            new Dog { Id = _dogA, OwnerId = _userA, Name = "Rex", Breed = "Poodle", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new Dog { Id = _dogB, OwnerId = _userB, Name = "Luna", Breed = "Labrador", IsActive = true, CreatedAtUtc = DateTime.UtcNow });

        _dbContext.SaveChanges();
    }

    public void Dispose() => _dbContext.Dispose();

    // ── Ownership verification ────────────────────────────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_ThrowsUnauthorized_WhenDogDoesNotBelongToUser()
    {
        // UserB tries to swipe using dogA (owned by userA)
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userB, CancellationToken.None));
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_IsIdempotent_WhenCalledTwice()
    {
        var first = await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);
        var second = await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);

        Assert.Equal(first.SwipeId, second.SwipeId);
        Assert.Equal(1, _dbContext.Swipes.Count());
    }

    // ── Dislike does not create match ─────────────────────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_DislikeDoesNotCreateMatch_EvenWithReciprocal()
    {
        // A dislikes B, B likes A
        await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: false, _userA, CancellationToken.None);
        var result = await _swipeService.RecordSwipeAsync(_dogB, _dogA, isLike: true, _userB, CancellationToken.None);

        Assert.False(result.MatchCreated);
        Assert.Null(result.MatchId);
        Assert.Equal(0, _dbContext.Matches.Count());
    }

    // ── Like without reciprocal does not create match ────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_LikeWithoutReciprocal_DoesNotCreateMatch()
    {
        var result = await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);

        Assert.False(result.MatchCreated);
        Assert.Null(result.MatchId);
        Assert.Equal(0, _dbContext.Matches.Count());
    }

    // ── Mutual like creates exactly one match ─────────────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_MutualLike_CreatesMatch()
    {
        await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);
        var result = await _swipeService.RecordSwipeAsync(_dogB, _dogA, isLike: true, _userB, CancellationToken.None);

        Assert.True(result.MatchCreated);
        Assert.NotNull(result.MatchId);
        Assert.Equal(1, _dbContext.Matches.Count());
    }

    // ── Match uses canonical ordering ────────────────────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_MutualLike_MatchHasCanonicalOrdering()
    {
        await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);
        var result = await _swipeService.RecordSwipeAsync(_dogB, _dogA, isLike: true, _userB, CancellationToken.None);

        var match = _dbContext.Matches.Single();
        var expectedA = _dogA < _dogB ? _dogA : _dogB;
        var expectedB = _dogA < _dogB ? _dogB : _dogA;

        Assert.Equal(expectedA, match.DogAId);
        Assert.Equal(expectedB, match.DogBId);
    }

    // ── No duplicate match on repeated mutual swipes ─────────────────────────

    [Fact]
    public async Task RecordSwipeAsync_MutualLike_DoesNotDuplicateMatch()
    {
        await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);
        await _swipeService.RecordSwipeAsync(_dogB, _dogA, isLike: true, _userB, CancellationToken.None);

        // Re-swipe (idempotent) — should not create second match
        await _swipeService.RecordSwipeAsync(_dogA, _dogB, isLike: true, _userA, CancellationToken.None);

        Assert.Equal(1, _dbContext.Matches.Count());
    }
}
