namespace SchoolAdmission.Api.Domain;

/// <summary>
/// Represents an admission unit for a class and shift.
/// </summary>
public sealed class AdmissionClass
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<AdmissionHold> Holds { get; set; } = new List<AdmissionHold>();
}
