using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Dogs;
using Matchcota.Services.Dogs.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Tests.Dogs;

public sealed class DogsServiceTests : IDisposable
{
    private readonly MatchcotaDbContext _dbContext;
    private readonly DogsService _dogsService;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _otherOwnerId = Guid.NewGuid();
    private readonly Guid _dogId = Guid.NewGuid();

    public DogsServiceTests()
    {
        var options = new DbContextOptionsBuilder<MatchcotaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new MatchcotaDbContext(options);
        _dogsService = new DogsService(_dbContext);

        SeedData();
    }

    public void Dispose() => _dbContext.Dispose();

    // ── GetMyDogsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyDogsAsync_ReturnsOnlyOwnerDogs()
    {
        var dogs = await _dogsService.GetMyDogsAsync(_ownerId, CancellationToken.None);

        Assert.Single(dogs);
        Assert.Equal(_dogId, dogs[0].DogId);
    }

    [Fact]
    public async Task GetMyDogsAsync_IncludesInactiveDogs()
    {
        var dog = await _dbContext.Dogs.FindAsync(_dogId);
        dog!.IsActive = false;
        await _dbContext.SaveChangesAsync();

        var dogs = await _dogsService.GetMyDogsAsync(_ownerId, CancellationToken.None);

        Assert.Single(dogs);
        Assert.False(dogs[0].IsActive);
    }

    // ── UpdateDogAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDogAsync_UpdatesFields_WhenOwner()
    {
        var request = new UpdateDogRequest("Max", "Poodle", "Juguetón", null, 18.5, -69.9);

        var result = await _dogsService.UpdateDogAsync(_ownerId, _dogId, request, CancellationToken.None);

        Assert.Equal("Max", result.Name);
        Assert.Equal("Poodle", result.Breed);
        Assert.Equal("Juguetón", result.Bio);
        Assert.Equal(18.5, result.Latitude);
        Assert.Equal(-69.9, result.Longitude);

        var persisted = await _dbContext.Dogs.FindAsync(_dogId);
        Assert.Equal("Max", persisted!.Name);
    }

    [Fact]
    public async Task UpdateDogAsync_ThrowsUnauthorized_WhenNotOwner()
    {
        var request = new UpdateDogRequest("Hack", "Unknown", "", null, null, null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _dogsService.UpdateDogAsync(_otherOwnerId, _dogId, request, CancellationToken.None));
    }

    // ── SetDogStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SetDogStatusAsync_DeactivatesDog_WhenOwner()
    {
        await _dogsService.SetDogStatusAsync(_ownerId, _dogId, isActive: false, CancellationToken.None);

        var dog = await _dbContext.Dogs.FindAsync(_dogId);
        Assert.False(dog!.IsActive);
    }

    [Fact]
    public async Task SetDogStatusAsync_ActivatesDog_WhenPreviouslyInactive()
    {
        var dog = await _dbContext.Dogs.FindAsync(_dogId);
        dog!.IsActive = false;
        await _dbContext.SaveChangesAsync();

        await _dogsService.SetDogStatusAsync(_ownerId, _dogId, isActive: true, CancellationToken.None);

        var updated = await _dbContext.Dogs.FindAsync(_dogId);
        Assert.True(updated!.IsActive);
    }

    [Fact]
    public async Task SetDogStatusAsync_ThrowsUnauthorized_WhenNotOwner()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _dogsService.SetDogStatusAsync(_otherOwnerId, _dogId, isActive: false, CancellationToken.None));
    }

    // ── Seed ─────────────────────────────────────────────────────────────────

    private void SeedData()
    {
        _dbContext.Users.AddRange(
            new User
            {
                Id = _ownerId,
                Email = "owner@matchcota.test",
                DisplayName = "Owner",
                PasswordHash = "x",
            },
            new User
            {
                Id = _otherOwnerId,
                Email = "other@matchcota.test",
                DisplayName = "Other",
                PasswordHash = "x",
            });

        _dbContext.Dogs.Add(new Dog
        {
            Id = _dogId,
            OwnerId = _ownerId,
            Name = "Rocky",
            Breed = "Golden Retriever",
            Bio = "Le encanta el parque",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        });

        _dbContext.SaveChanges();
    }
}
