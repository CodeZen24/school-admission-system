namespace SchoolAdmission.Api.Features.Holds;

/// <summary>
/// Response payload for a newly created seat hold.
/// </summary>
public sealed record HoldResponse(
    Guid HoldId,
    Guid ClassId,
    string StudentId,
    string Status,
    DateTimeOffset ExpiresAt);
