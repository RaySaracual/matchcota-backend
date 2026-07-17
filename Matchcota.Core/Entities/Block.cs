namespace Matchcota.Core.Entities;

public sealed class Block
{
    public Guid Id { get; set; }
    public Guid BlockerUserId { get; set; }
    public Guid BlockedDogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public User BlockerUser { get; set; } = default!;
    public Dog BlockedDog { get; set; } = default!;
}
