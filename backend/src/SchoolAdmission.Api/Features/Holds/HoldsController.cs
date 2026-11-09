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
}
