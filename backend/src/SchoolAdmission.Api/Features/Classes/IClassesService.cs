namespace SchoolAdmission.Api.Features.Classes;

public interface IClassesService
{
    Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClassDetailsResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClassDetailsResponse?> GetByIdAsync(Guid classId, CancellationToken cancellationToken);
}
