namespace SchoolAdmission.Api.Features.Holds;

public interface IHoldsService
{
    Task<ApplyHoldResult> ApplyAsync(Guid classId, string studentId, CancellationToken cancellationToken);

    Task<HoldDetailsResponse?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken);

    Task<ConfirmHoldResult> ConfirmAsync(Guid holdId, string studentId, CancellationToken cancellationToken);

    Task<CancelHoldResult> CancelAsync(Guid holdId, string studentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentHoldResponse>> GetMyHoldsAsync(string studentId, CancellationToken cancellationToken);
}
