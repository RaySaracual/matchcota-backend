namespace Matchcota.Core.Entities;

public sealed class Swipe
{
    public Guid Id { get; set; }
    public Guid SourceDogId { get; set; }
    public Guid TargetDogId { get; set; }
    public bool IsLike { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Dog SourceDog { get; set; } = default!;
    public Dog TargetDog { get; set; } = default!;
}
