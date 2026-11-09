using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Features.Classes;

public sealed class ClassesService(SchoolAdmissionDbContext dbContext) : IClassesService
{
    private readonly SchoolAdmissionDbContext _dbContext = dbContext;

    public async Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Classes.Add(admissionClass);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ClassResponse(
            admissionClass.Id,
            admissionClass.Name,
            admissionClass.TotalSeats);
    }

    public async Task<IReadOnlyList<ClassDetailsResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new ClassDetailsResponse(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                c.Holds.Count(h => h.Status == HoldStatus.Confirmed),
                Math.Max(0, c.TotalSeats - (c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now) + c.Holds.Count(h => h.Status == HoldStatus.Confirmed)))))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassDetailsResponse?> GetByIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new ClassDetailsResponse(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                c.Holds.Count(h => h.Status == HoldStatus.Confirmed),
                Math.Max(0, c.TotalSeats - (c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now) + c.Holds.Count(h => h.Status == HoldStatus.Confirmed)))))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
