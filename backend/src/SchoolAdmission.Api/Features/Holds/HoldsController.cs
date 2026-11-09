using Microsoft.AspNetCore.Mvc;

namespace SchoolAdmission.Api.Features.Holds;

[ApiController]
public sealed class HoldsController(IHoldsService holdsService) : ControllerBase
{
    private readonly IHoldsService _holdsService = holdsService;

    [HttpPost("classes/{classId}/holds")]
    [ProducesResponseType<HoldResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply(
        Guid classId,
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken)
    {
        if (!StudentIdentity.IsValid(studentId))
        {
            ModelState.AddModelError(
                StudentIdentity.HeaderName,
                $"The {StudentIdentity.HeaderName} header is required and must be exactly {StudentIdentity.Length} digits.");
            return ValidationProblem(ModelState);
        }

        var result = await _holdsService.ApplyAsync(classId, studentId, cancellationToken);

        return result.Outcome switch
        {
            ApplyHoldOutcome.Created => StatusCode(StatusCodes.Status201Created, result.Hold),
            ApplyHoldOutcome.ClassNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Class not found.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5"),
            ApplyHoldOutcome.ClassFull => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The class has no available seats.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10"),
            ApplyHoldOutcome.DuplicateApplication => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The student already has an active or confirmed application for this class.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10"),
            _ => throw new InvalidOperationException($"Unhandled outcome {result.Outcome}.")
        };
    }

    [HttpGet("holds/{holdId}")]
    [ProducesResponseType<HoldDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HoldDetailsResponse>> GetById(
        Guid holdId,
        CancellationToken cancellationToken)
    {
        var hold = await _holdsService.GetByIdAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Hold not found.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5");
        }

        return Ok(hold);
    }

    [HttpPost("holds/{holdId}/confirm")]
    [ProducesResponseType<ConfirmHoldResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        Guid holdId,
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken)
    {
        if (!StudentIdentity.IsValid(studentId))
        {
            ModelState.AddModelError(
                StudentIdentity.HeaderName,
                $"The {StudentIdentity.HeaderName} header is required and must be exactly {StudentIdentity.Length} digits.");
            return ValidationProblem(ModelState);
        }

        var result = await _holdsService.ConfirmAsync(holdId, studentId, cancellationToken);

        return result.Outcome switch
        {
            ConfirmHoldOutcome.Confirmed => Ok(result.Response),
            ConfirmHoldOutcome.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Hold not found.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5"),
            ConfirmHoldOutcome.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Only the student who applied can confirm this hold.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4"),
            ConfirmHoldOutcome.Expired => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The hold has expired and cannot be confirmed.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10"),
            ConfirmHoldOutcome.Cancelled => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The hold has been cancelled and cannot be confirmed.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10"),
            _ => throw new InvalidOperationException($"Unhandled outcome {result.Outcome}.")
        };
    }

    [HttpDelete("holds/{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid holdId,
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken)
    {
        if (!StudentIdentity.IsValid(studentId))
        {
            ModelState.AddModelError(
                StudentIdentity.HeaderName,
                $"The {StudentIdentity.HeaderName} header is required and must be exactly {StudentIdentity.Length} digits.");
            return ValidationProblem(ModelState);
        }

        var result = await _holdsService.CancelAsync(holdId, studentId, cancellationToken);

        return result.Outcome switch
        {
            CancelHoldOutcome.Cancelled => NoContent(),
            CancelHoldOutcome.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Hold not found.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5"),
            CancelHoldOutcome.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Only the student who applied can cancel this hold.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4"),
            CancelHoldOutcome.ConflictConfirmed => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Confirmed admission cannot be cancelled.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10"),
            _ => throw new InvalidOperationException($"Unhandled outcome {result.Outcome}.")
        };
    }

    [HttpGet("me/holds")]
    [ProducesResponseType<IReadOnlyList<StudentHoldResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StudentHoldResponse>>> GetMyHolds(
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken)
    {
        if (!StudentIdentity.IsValid(studentId))
        {
            ModelState.AddModelError(
                StudentIdentity.HeaderName,
                $"The {StudentIdentity.HeaderName} header is required and must be exactly {StudentIdentity.Length} digits.");
            return ValidationProblem(ModelState);
        }

        var holds = await _holdsService.GetMyHoldsAsync(studentId, cancellationToken);
        return Ok(holds);
    }
}
