namespace SchoolAdmission.Api.Features.Holds;

public enum ConfirmHoldOutcome
{
    Confirmed,
    NotFound,
    Forbidden,
    Expired,
    Cancelled
}

/// <summary>
/// Outcome of a hold confirmation request.
/// </summary>
public sealed record ConfirmHoldResult(ConfirmHoldOutcome Outcome, ConfirmHoldResponse? Response = null);
