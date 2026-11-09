namespace SchoolAdmission.Api.Features.Holds;

/// <summary>
/// Response payload for inspecting a single hold.
/// </summary>
public sealed record HoldDetailsResponse(
    Guid HoldId,
    Guid ClassId,
    string Status,
    DateTimeOffset ExpiresAt);
