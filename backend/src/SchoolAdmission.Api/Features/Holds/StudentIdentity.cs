using System.Diagnostics.CodeAnalysis;

namespace SchoolAdmission.Api.Features.Holds;

/// <summary>
/// Validates the trusted <c>X-Student-Id</c> header. The value identifies ownership but is not authentication.
/// </summary>
public static class StudentIdentity
{
    public const string HeaderName = "X-Student-Id";

    public const int Length = 17;

    /// <summary>
    /// A valid student ID is exactly 17 ASCII digits. Unicode digits are rejected on purpose
    /// because the database check constraint accepts only <c>[0-9]</c>.
    /// </summary>
    public static bool IsValid([NotNullWhen(true)] string? value) =>
        value is { Length: Length } && value.All(char.IsAsciiDigit);
}
