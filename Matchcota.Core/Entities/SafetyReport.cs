namespace Matchcota.Core.Entities;

public sealed class SafetyReport
{
    public Guid Id { get; set; }
    public Guid ReportedByUserId { get; set; }
    public Guid ReportedDogId { get; set; }

    /// <summary>SPAM | INAPPROPRIATE | AGGRESSIVE | FAKE | OTHER</summary>
    public string Category { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public User ReportedByUser { get; set; } = default!;
    public Dog ReportedDog { get; set; } = default!;
}
