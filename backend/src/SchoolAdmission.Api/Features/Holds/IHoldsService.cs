namespace SchoolAdmission.Api.Features.Holds;

public interface IHoldsService
{
    Task<ApplyHoldResult> ApplyAsync(Guid classId, string studentId, CancellationToken cancellationToken);
}
