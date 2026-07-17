using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Services.Safety;

public sealed class SafetyService(MatchcotaDbContext dbContext) : ISafetyService
{
    private static readonly IReadOnlySet<string> ValidCategories =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SPAM", "INAPPROPRIATE", "AGGRESSIVE", "FAKE", "OTHER"
        };

    private readonly MatchcotaDbContext _dbContext = dbContext;

    public async Task ReportAsync(
        Guid reportedByUserId,
        Guid reportedDogId,
        string category,
        string? detail,
        CancellationToken cancellationToken)
    {
        var normalizedCategory = (category ?? string.Empty).Trim().ToUpperInvariant();
        if (!ValidCategories.Contains(normalizedCategory))
        {
            throw new ArgumentException($"Invalid report category '{category}'.", nameof(category));
        }

        var dogExists = await _dbContext.Dogs
            .AsNoTracking()
            .AnyAsync(d => d.Id == reportedDogId && d.IsActive, cancellationToken);

        if (!dogExists)
        {
            throw new KeyNotFoundException($"Dog {reportedDogId} not found.");
        }

        var report = new SafetyReport
        {
            Id = Guid.NewGuid(),
            ReportedByUserId = reportedByUserId,
            ReportedDogId = reportedDogId,
            Category = normalizedCategory,
            Detail = detail?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.SafetyReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BlockAsync(
        Guid blockerUserId,
        Guid blockedDogId,
        CancellationToken cancellationToken)
    {
        var dogExists = await _dbContext.Dogs
            .AsNoTracking()
            .AnyAsync(d => d.Id == blockedDogId && d.IsActive, cancellationToken);

        if (!dogExists)
        {
            throw new KeyNotFoundException($"Dog {blockedDogId} not found.");
        }

        var alreadyBlocked = await _dbContext.Blocks
            .AsNoTracking()
            .AnyAsync(b => b.BlockerUserId == blockerUserId && b.BlockedDogId == blockedDogId, cancellationToken);

        if (alreadyBlocked)
        {
            return; // idempotent
        }

        _dbContext.Blocks.Add(new Block
        {
            Id = Guid.NewGuid(),
            BlockerUserId = blockerUserId,
            BlockedDogId = blockedDogId,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UnblockAsync(
        Guid blockerUserId,
        Guid blockedDogId,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Blocks
            .FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedDogId == blockedDogId, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        _dbContext.Blocks.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<Guid>> GetBlockedDogIdsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var ids = await _dbContext.Blocks
            .AsNoTracking()
            .Where(b => b.BlockerUserId == userId)
            .Select(b => b.BlockedDogId)
            .ToListAsync(cancellationToken);

        return ids;
    }
}
