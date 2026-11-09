namespace SchoolAdmission.Api.Domain;

/// <summary>
/// Supported statuses for an admission hold.
/// </summary>
public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Cancelled = "CANCELLED";
    public const string Expired = "EXPIRED";

    public static readonly string[] All = [Held, Confirmed, Cancelled, Expired];
}
