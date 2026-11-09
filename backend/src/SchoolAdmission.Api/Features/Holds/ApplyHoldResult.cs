namespace SchoolAdmission.Api.Features.Holds;

public enum ApplyHoldOutcome
{
    Created,
    ClassNotFound,
    ClassFull,
    DuplicateApplication
}

/// <summary>
/// Outcome of a seat application. Expected business rejections are results, not exceptions.
/// </summary>
public sealed record ApplyHoldResult(ApplyHoldOutcome Outcome, HoldResponse? Hold = null);
