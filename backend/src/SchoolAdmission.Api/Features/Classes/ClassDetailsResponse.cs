namespace SchoolAdmission.Api.Features.Classes;

/// <summary>
/// Response payload for class details including dynamic seat availability.
/// </summary>
public sealed record ClassDetailsResponse(
    Guid Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
