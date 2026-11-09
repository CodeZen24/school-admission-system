namespace SchoolAdmission.Api.Domain;

/// <summary>
/// Represents a temporary or finalized seat reservation for a student in a class.
/// </summary>
public sealed class AdmissionHold
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public AdmissionClass Class { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;

    public string Status { get; set; } = HoldStatus.Held;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
