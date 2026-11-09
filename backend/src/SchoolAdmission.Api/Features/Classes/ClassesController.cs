using Microsoft.AspNetCore.Mvc;

namespace SchoolAdmission.Api.Features.Classes;

[ApiController]
[Route("classes")]
public sealed class ClassesController(IClassesService classesService) : ControllerBase
{
    private readonly IClassesService _classesService = classesService;

    [HttpPost]
    [ProducesResponseType<ClassResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "The Name field cannot be empty or whitespace.");
            return ValidationProblem(ModelState);
        }

        if (request.TotalSeats <= 0)
        {
            ModelState.AddModelError(nameof(request.TotalSeats), "The TotalSeats field must be greater than zero.");
            return ValidationProblem(ModelState);
        }

        var created = await _classesService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { classId = created.Id },
            created);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClassDetailsResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClassDetailsResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var classes = await _classesService.GetAllAsync(cancellationToken);
        return Ok(classes);
    }

    [HttpGet("{classId:guid}")]
    [ProducesResponseType<ClassDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassDetailsResponse>> GetById(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var admissionClass = await _classesService.GetByIdAsync(classId, cancellationToken);
        if (admissionClass is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Class not found.",
                type: "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5");
        }

        return Ok(admissionClass);
    }
}
