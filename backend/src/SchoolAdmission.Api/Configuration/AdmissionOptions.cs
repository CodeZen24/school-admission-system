using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Configuration;

internal sealed class AdmissionOptions
{
    internal const string SectionName = "Admission";

    [Range(1, 86_400)]
    public int HoldDurationSeconds { get; set; } = 600;
}
