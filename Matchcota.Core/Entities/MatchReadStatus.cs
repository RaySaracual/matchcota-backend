namespace Matchcota.Core.Entities;

public sealed class MatchReadStatus
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid UserId { get; set; }
    public DateTime LastReadAtUtc { get; set; }

    public Match Match { get; set; } = default!;
    public User User { get; set; } = default!;
}
