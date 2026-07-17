namespace Matchcota.Core.Entities;

public sealed class DogMedia
{
    public Guid Id { get; set; }
    public Guid DogId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public Dog Dog { get; set; } = default!;
}
