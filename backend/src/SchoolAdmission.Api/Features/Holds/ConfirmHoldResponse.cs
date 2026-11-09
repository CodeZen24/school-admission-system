namespace SchoolAdmission.Api.Features.Holds;

/// <summary>
/// Response payload for confirming a hold.
/// </summary>
public sealed record ConfirmHoldResponse(
    Guid HoldId,
    string Status);
