namespace SchoolAdmission.Api.Features.Holds;

public enum CancelHoldOutcome
{
    Cancelled,
    NotFound,
    Forbidden,
    ConflictConfirmed
}

/// <summary>
/// Outcome of a hold cancellation request.
/// </summary>
public sealed record CancelHoldResult(CancelHoldOutcome Outcome);
