using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Chat;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Tests.Chat;

public sealed class ChatServiceTests : IDisposable
{
    private readonly MatchcotaDbContext _dbContext;
    private readonly ChatService _chatService;

    private readonly Guid _ownerAId = Guid.NewGuid();
    private readonly Guid _ownerBId = Guid.NewGuid();
    private readonly Guid _intruderId = Guid.NewGuid();

    private readonly Guid _dogAId = Guid.NewGuid();
    private readonly Guid _dogBId = Guid.NewGuid();
    private readonly Guid _matchId = Guid.NewGuid();

    public ChatServiceTests()
    {
        var options = new DbContextOptionsBuilder<MatchcotaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new MatchcotaDbContext(options);
        _chatService = new ChatService(_dbContext);

        SeedData();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SendMessageAsync_PersistsMessage_WhenUserBelongsToMatch()
    {
        var sent = await _chatService.SendMessageAsync(
            _ownerAId,
            _matchId,
            _dogAId,
            "Hola desde el test",
            CancellationToken.None);

        var persisted = await _dbContext.Messages.FirstOrDefaultAsync(m => m.Id == sent.MessageId);

        Assert.NotNull(persisted);
        Assert.Equal(_matchId, persisted!.MatchId);
        Assert.Equal(_dogAId, persisted.SenderDogId);
        Assert.Equal("Hola desde el test", persisted.Content);
    }

    [Fact]
    public async Task EnsureUserBelongsToMatchAsync_ThrowsUnauthorized_ForIntruder()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _chatService.EnsureUserBelongsToMatchAsync(_intruderId, _matchId, CancellationToken.None));
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsMessagesInAscendingChronologicalOrder()
    {
        await _chatService.SendMessageAsync(_ownerAId, _matchId, _dogAId, "primer", CancellationToken.None);
        await Task.Delay(2);
        await _chatService.SendMessageAsync(_ownerBId, _matchId, _dogBId, "segundo", CancellationToken.None);

        var page = await _chatService.GetMessagesAsync(
            _ownerAId,
            _matchId,
            page: 1,
            pageSize: 30,
            CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal("primer", page.Items[0].Content);
        Assert.Equal("segundo", page.Items[1].Content);
    }

    private void SeedData()
    {
        _dbContext.Users.AddRange(
            new User { Id = _ownerAId, Email = "owner-a@matchcota.test", DisplayName = "Owner A", PasswordHash = "x" },
            new User { Id = _ownerBId, Email = "owner-b@matchcota.test", DisplayName = "Owner B", PasswordHash = "x" },
            new User { Id = _intruderId, Email = "intruder@matchcota.test", DisplayName = "Intruder", PasswordHash = "x" });

        _dbContext.Dogs.AddRange(
            new Dog
            {
                Id = _dogAId,
                OwnerId = _ownerAId,
                Name = "Rex",
                Breed = "Poodle",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            },
            new Dog
            {
                Id = _dogBId,
                OwnerId = _ownerBId,
                Name = "Luna",
                Breed = "Labrador",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            });

        _dbContext.Matches.Add(new Match
        {
            Id = _matchId,
            DogAId = _dogAId,
            DogBId = _dogBId,
            IsActive = true,
            MatchedAtUtc = DateTime.UtcNow,
        });

        _dbContext.SaveChanges();
    }
}
