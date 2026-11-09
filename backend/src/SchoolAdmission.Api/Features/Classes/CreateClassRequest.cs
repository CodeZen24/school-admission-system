using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Features.Classes;

/// <summary>
/// Request payload to create a new class/shift admission unit.
/// </summary>
public sealed record CreateClassRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [Range(1, int.MaxValue)]
    int TotalSeats);
