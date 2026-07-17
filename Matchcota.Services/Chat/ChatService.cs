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

    public async Task<IReadOnlyList<ConversationResult>> GetConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<Guid>? blockedDogIds = null)
    {
        var userDogIds = await _dbContext.Dogs
            .AsNoTracking()
            .Where(d => d.OwnerId == userId && d.IsActive)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        if (userDogIds.Count == 0)
            return Array.Empty<ConversationResult>();

        var matches = await _dbContext.Matches
            .AsNoTracking()
            .Where(m => m.IsActive && (userDogIds.Contains(m.DogAId) || userDogIds.Contains(m.DogBId)))
            .Select(m => new { m.Id, m.DogAId, m.DogBId })
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
            return Array.Empty<ConversationResult>();

        var matchIds = matches.Select(m => m.Id).ToList();

        var matchMeta = matches.Select(m =>
        {
            var mine = userDogIds.Contains(m.DogAId) ? m.DogAId : m.DogBId;
            var other = mine == m.DogAId ? m.DogBId : m.DogAId;
            return (MatchId: m.Id, MyDogId: mine, OtherDogId: other);
        }).ToList();

        var otherDogIds = matchMeta.Select(x => x.OtherDogId).Distinct().ToList();

        var otherDogs = await _dbContext.Dogs
            .AsNoTracking()
            .Where(d => otherDogIds.Contains(d.Id))
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Breed,
                PhotoUrl = d.Media
                    .OrderBy(m => m.CreatedAtUtc)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var dogMap = otherDogs.ToDictionary(d => d.Id);

        // Load messages for all matches to compute last message and unread count.
        // Acceptable at beta scale; add DB-side aggregation if needed later.
        var allMessages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => matchIds.Contains(m.MatchId))
            .Select(m => new { m.MatchId, m.SenderDogId, m.Content, m.SentAtUtc })
            .ToListAsync(cancellationToken);

        var readStatuses = await _dbContext.MatchReadStatuses
            .AsNoTracking()
            .Where(r => r.UserId == userId && matchIds.Contains(r.MatchId))
            .ToDictionaryAsync(r => r.MatchId, r => r.LastReadAtUtc, cancellationToken);

        var results = matchMeta
            .Where(meta => blockedDogIds is null || !blockedDogIds.Contains(meta.OtherDogId))
            .Select(meta =>
        {
            var msgs = allMessages.Where(m => m.MatchId == meta.MatchId).ToList();
            var lastMsg = msgs.Count > 0 ? msgs.MaxBy(m => m.SentAtUtc) : null;
            readStatuses.TryGetValue(meta.MatchId, out var lastReadAt);
            var unread = msgs.Count(m => m.SenderDogId != meta.MyDogId && m.SentAtUtc > lastReadAt);

            dogMap.TryGetValue(meta.OtherDogId, out var dog);

            return new ConversationResult(
                meta.MatchId,
                meta.OtherDogId,
                dog?.Name ?? string.Empty,
                dog?.Breed ?? string.Empty,
                dog?.PhotoUrl,
                lastMsg?.Content,
                lastMsg?.SentAtUtc,
                meta.MyDogId,
                unread);
        })
        .OrderByDescending(r => r.LastMessageSentAtUtc)
        .ToList();

        return results;
    }

    public async Task MarkAsReadAsync(
        Guid userId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var match = await GetAuthorizedMatchAsync(userId, matchId, cancellationToken);
        if (match is null)
            throw new UnauthorizedAccessException("The user cannot access this match chat.");

        var existing = await _dbContext.MatchReadStatuses
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MatchId == matchId, cancellationToken);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            _dbContext.MatchReadStatuses.Add(new MatchReadStatus
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                UserId = userId,
                LastReadAtUtc = now,
            });
        }
        else
        {
            existing.LastReadAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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
