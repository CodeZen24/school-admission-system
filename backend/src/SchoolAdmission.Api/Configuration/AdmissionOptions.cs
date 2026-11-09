using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Configuration;

public sealed class AdmissionOptions
{
    public const string SectionName = "Admission";

    [Range(1, 86_400)]
    public int HoldDurationSeconds { get; set; } = 600;
}
