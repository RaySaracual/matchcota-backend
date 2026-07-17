namespace Matchcota.Core.Entities;

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid SenderDogId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }

    public Match Match { get; set; } = default!;
    public Dog SenderDog { get; set; } = default!;
}
