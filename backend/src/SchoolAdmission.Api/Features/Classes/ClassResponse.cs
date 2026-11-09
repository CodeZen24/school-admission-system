namespace SchoolAdmission.Api.Features.Classes;

/// <summary>
/// Response payload for a newly created admission class.
/// </summary>
public sealed record ClassResponse(
    Guid Id,
    string Name,
    int TotalSeats);
