namespace SchoolAdmission.Api.Features.Holds;

/// <summary>
/// Response payload for a student's hold history item.
/// </summary>
public sealed record StudentHoldResponse(
    Guid HoldId,
    Guid ClassId,
    string ClassName,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
