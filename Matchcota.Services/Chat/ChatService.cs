using Matchcota.Core.Entities;
using Matchcota.Infrastructure.Persistence;
using Matchcota.Services.Chat.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Services.Chat;

public sealed class ChatService(MatchcotaDbContext dbContext) : IChatService
{
    private readonly MatchcotaDbContext _dbContext = dbContext;

    public async Task EnsureUserBelongsToMatchAsync(
        Guid userId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var match = await GetAuthorizedMatchAsync(userId, matchId, cancellationToken);
        if (match is null)
        {
            throw new UnauthorizedAccessException("The user cannot access this match chat.");
        }
    }

    public async Task<PagedMessagesResult> GetMessagesAsync(
        Guid userId,
        Guid matchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var match = await GetAuthorizedMatchAsync(userId, matchId, cancellationToken);
        if (match is null)
        {
            throw new UnauthorizedAccessException("The user cannot access this match chat.");
        }

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 30 : Math.Min(pageSize, 100);
        var skip = (safePage - 1) * safePageSize;

        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.MatchId == matchId)
            .OrderByDescending(m => m.SentAtUtc)
            .Skip(skip)
            .Take(safePageSize + 1)
            .Select(m => new ChatMessageResult(
                m.Id,
                m.MatchId,
                m.SenderDogId,
                m.Content,
                m.SentAtUtc))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > safePageSize;
        var pageItems = hasMore ? messages.Take(safePageSize).ToList() : messages;

        pageItems.Reverse();

        return new PagedMessagesResult(pageItems, safePage, safePageSize, hasMore);
    }

    public async Task<ChatMessageResult> SendMessageAsync(
        Guid userId,
        Guid matchId,
        Guid senderDogId,
        string content,
        CancellationToken cancellationToken)
    {
        var match = await GetAuthorizedMatchAsync(userId, matchId, cancellationToken);
        if (match is null)
        {
            throw new UnauthorizedAccessException("The user cannot access this match chat.");
        }

        var normalized = (content ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        if (normalized.Length > 4000)
        {
            throw new ArgumentException("Message content exceeds maximum length.", nameof(content));
        }

        if (senderDogId != match.DogAId && senderDogId != match.DogBId)
        {
            throw new UnauthorizedAccessException("Sender dog does not belong to this match.");
        }

        var senderOwned = await _dbContext.Dogs
            .AsNoTracking()
            .AnyAsync(d => d.Id == senderDogId && d.OwnerId == userId && d.IsActive, cancellationToken);

        if (!senderOwned)
        {
            throw new UnauthorizedAccessException("Sender dog does not belong to the requesting user.");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            SenderDogId = senderDogId,
            Content = normalized,
            SentAtUtc = DateTime.UtcNow,
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ChatMessageResult(
            message.Id,
            message.MatchId,
            message.SenderDogId,
            message.Content,
            message.SentAtUtc);
    }

    private async Task<AuthorizedMatch?> GetAuthorizedMatchAsync(
        Guid userId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var match = await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.Id == matchId && m.IsActive)
            .Select(m => new { m.DogAId, m.DogBId })
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return null;
        }

        var owners = await _dbContext.Dogs
            .AsNoTracking()
            .Where(d => d.Id == match.DogAId || d.Id == match.DogBId)
            .Select(d => new { d.Id, d.OwnerId })
            .ToListAsync(cancellationToken);

        var dogAOwner = owners.FirstOrDefault(o => o.Id == match.DogAId)?.OwnerId;
        var dogBOwner = owners.FirstOrDefault(o => o.Id == match.DogBId)?.OwnerId;

        if (dogAOwner is null || dogBOwner is null)
        {
            return null;
        }

        if (dogAOwner != userId && dogBOwner != userId)
        {
            return null;
        }

        return new AuthorizedMatch(match.DogAId, match.DogBId, dogAOwner.Value, dogBOwner.Value);
    }

    private sealed record AuthorizedMatch(
        Guid DogAId,
        Guid DogBId,
        Guid DogAOwnerId,
        Guid DogBOwnerId);
}
