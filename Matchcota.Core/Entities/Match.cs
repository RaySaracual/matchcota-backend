namespace Matchcota.Core.Entities;

public sealed class Match
{
    public Guid Id { get; set; }
    public Guid DogAId { get; set; }
    public Guid DogBId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime MatchedAtUtc { get; set; }

    public Dog DogA { get; set; } = default!;
    public Dog DogB { get; set; } = default!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
