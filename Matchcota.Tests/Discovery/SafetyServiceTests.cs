using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Safety;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Tests.Discovery;

public sealed class SafetyServiceTests : IDisposable
{
    private readonly MatchcotaDbContext _dbContext;
    private readonly SafetyService _safetyService;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _dogId = Guid.NewGuid();

    public SafetyServiceTests()
    {
        var options = new DbContextOptionsBuilder<MatchcotaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new MatchcotaDbContext(options);
        _safetyService = new SafetyService(_dbContext);

        SeedData();
    }

    public void Dispose() => _dbContext.Dispose();

    // ── Report tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReportAsync_PersistsReport_WithValidCategory()
    {
        await _safetyService.ReportAsync(
            _userId, _dogId, "SPAM", "Anuncio de venta de cachorros", CancellationToken.None);

        var report = await _dbContext.SafetyReports.FirstOrDefaultAsync();

        Assert.NotNull(report);
        Assert.Equal(_userId, report!.ReportedByUserId);
        Assert.Equal(_dogId, report.ReportedDogId);
        Assert.Equal("SPAM", report.Category);
        Assert.Equal("Anuncio de venta de cachorros", report.Detail);
    }

    [Fact]
    public async Task ReportAsync_Throws_ForInvalidCategory()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _safetyService.ReportAsync(_userId, _dogId, "INVALID_CAT", null, CancellationToken.None));
    }

    [Fact]
    public async Task ReportAsync_Throws_WhenDogNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _safetyService.ReportAsync(_userId, Guid.NewGuid(), "OTHER", null, CancellationToken.None));
    }

    // ── Block tests ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task BlockAsync_CreatesBlock_WhenNotAlreadyBlocked()
    {
        await _safetyService.BlockAsync(_userId, _dogId, CancellationToken.None);

        var block = await _dbContext.Blocks.FirstOrDefaultAsync();

        Assert.NotNull(block);
        Assert.Equal(_userId, block!.BlockerUserId);
        Assert.Equal(_dogId, block.BlockedDogId);
    }

    [Fact]
    public async Task BlockAsync_IsIdempotent_WhenAlreadyBlocked()
    {
        await _safetyService.BlockAsync(_userId, _dogId, CancellationToken.None);
        await _safetyService.BlockAsync(_userId, _dogId, CancellationToken.None); // should not throw

        Assert.Equal(1, await _dbContext.Blocks.CountAsync());
    }

    [Fact]
    public async Task UnblockAsync_RemovesBlock_AndReturnsTrue()
    {
        await _safetyService.BlockAsync(_userId, _dogId, CancellationToken.None);

        var removed = await _safetyService.UnblockAsync(_userId, _dogId, CancellationToken.None);

        Assert.True(removed);
        Assert.Empty(_dbContext.Blocks);
    }

    [Fact]
    public async Task UnblockAsync_ReturnsFalse_WhenNotBlocked()
    {
        var removed = await _safetyService.UnblockAsync(_userId, _dogId, CancellationToken.None);

        Assert.False(removed);
    }

    [Fact]
    public async Task GetBlockedDogIdsAsync_ReturnsCorrectIds()
    {
        await _safetyService.BlockAsync(_userId, _dogId, CancellationToken.None);

        var blocked = await _safetyService.GetBlockedDogIdsAsync(_userId, CancellationToken.None);
        var otherBlocked = await _safetyService.GetBlockedDogIdsAsync(_otherUserId, CancellationToken.None);

        Assert.Contains(_dogId, blocked);
        Assert.Empty(otherBlocked);
    }

    private void SeedData()
    {
        _dbContext.Users.AddRange(
            new User { Id = _userId, Email = "safety-user@matchcota.test", DisplayName = "Safety User", PasswordHash = "x" },
            new User { Id = _otherUserId, Email = "other@matchcota.test", DisplayName = "Other", PasswordHash = "x" });

        _dbContext.Dogs.Add(new Dog
        {
            Id = _dogId,
            OwnerId = _otherUserId,
            Name = "TestDog",
            Breed = "Mixed",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        });

        _dbContext.SaveChanges();
    }
}
