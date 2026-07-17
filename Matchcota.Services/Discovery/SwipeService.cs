using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Discovery.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Services.Discovery;

public sealed class SwipeService(MatchcotaDbContext dbContext) : ISwipeService
{
    private readonly MatchcotaDbContext _dbContext = dbContext;

    public async Task<SwipeResult> RecordSwipeAsync(
        Guid sourceDogId,
        Guid targetDogId,
        bool isLike,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        // Verify ownership — source dog must belong to the requesting user
        var ownsSourceDog = await _dbContext.Dogs
            .AsNoTracking()
            .AnyAsync(d => d.Id == sourceDogId && d.OwnerId == requestingUserId, cancellationToken);

        if (!ownsSourceDog)
        {
            throw new UnauthorizedAccessException("Dog does not belong to the requesting user.");
        }

        // Idempotent: return existing swipe if already recorded
        var existing = await _dbContext.Swipes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.SourceDogId == sourceDogId && s.TargetDogId == targetDogId,
                cancellationToken);

        if (existing is not null)
        {
            var existingMatch = await _dbContext.Matches
                .AsNoTracking()
                .Where(m =>
                    (m.DogAId == sourceDogId && m.DogBId == targetDogId) ||
                    (m.DogAId == targetDogId && m.DogBId == sourceDogId))
                .Select(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return new SwipeResult(
                existing.Id,
                existingMatch != Guid.Empty,
                existingMatch == Guid.Empty ? null : existingMatch);
        }

        var swipe = new Swipe
        {
            Id = Guid.NewGuid(),
            SourceDogId = sourceDogId,
            TargetDogId = targetDogId,
            IsLike = isLike,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _dbContext.Swipes.Add(swipe);

        Guid? matchId = null;

        if (isLike)
        {
            // Check for reciprocal like
            var hasReciprocal = await _dbContext.Swipes
                .AsNoTracking()
                .AnyAsync(
                    s => s.SourceDogId == targetDogId && s.TargetDogId == sourceDogId && s.IsLike,
                    cancellationToken);

            if (hasReciprocal)
            {
                // Canonical ordering prevents duplicate match rows
                var dogAId = sourceDogId < targetDogId ? sourceDogId : targetDogId;
                var dogBId = sourceDogId < targetDogId ? targetDogId : sourceDogId;

                var alreadyMatched = await _dbContext.Matches
                    .AsNoTracking()
                    .AnyAsync(m => m.DogAId == dogAId && m.DogBId == dogBId, cancellationToken);

                if (!alreadyMatched)
                {
                    var match = new Match
                    {
                        Id = Guid.NewGuid(),
                        DogAId = dogAId,
                        DogBId = dogBId,
                        IsActive = true,
                        MatchedAtUtc = DateTime.UtcNow,
                    };
                    _dbContext.Matches.Add(match);
                    matchId = match.Id;
                }
                else
                {
                    matchId = await _dbContext.Matches
                        .AsNoTracking()
                        .Where(m => m.DogAId == dogAId && m.DogBId == dogBId)
                        .Select(m => (Guid?)m.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SwipeResult(swipe.Id, matchId.HasValue, matchId);
    }
}
