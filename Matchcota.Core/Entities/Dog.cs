using NetTopologySuite.Geometries;

namespace Matchcota.Core.Entities;

public sealed class Dog
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string Bio { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Point Location { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }

    public User Owner { get; set; } = default!;
    public ICollection<DogMedia> Media { get; set; } = new List<DogMedia>();
}
