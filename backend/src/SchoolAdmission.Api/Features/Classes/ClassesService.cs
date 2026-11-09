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
        var rows = await SeatCounts(_dbContext.Classes.AsNoTracking().OrderBy(c => c.CreatedAt))
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(ToResponse);
    }

    public async Task<ClassDetailsResponse?> GetByIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        var row = await SeatCounts(_dbContext.Classes.AsNoTracking().Where(c => c.Id == classId))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToResponse(row);
    }

    // Expiry is evaluated with PostgreSQL's clock (DateTimeOffset.UtcNow translates to now()),
    // so a delayed expiration worker never counts a past-due hold as live.
    private static IQueryable<ClassSeatCounts> SeatCounts(IQueryable<AdmissionClass> classes) =>
        classes.Select(c => new ClassSeatCounts(
            c.Id,
            c.Name,
            c.TotalSeats,
            c.CreatedAt,
            c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > DateTimeOffset.UtcNow),
            c.Holds.Count(h => h.Status == HoldStatus.Confirmed)));

    private static ClassDetailsResponse ToResponse(ClassSeatCounts row) => new(
        row.Id,
        row.Name,
        row.TotalSeats,
        row.HeldSeats,
        row.ConfirmedSeats,
        Math.Max(0, row.TotalSeats - row.HeldSeats - row.ConfirmedSeats));

    private sealed record ClassSeatCounts(
        Guid Id,
        string Name,
        int TotalSeats,
        DateTimeOffset CreatedAt,
        int HeldSeats,
        int ConfirmedSeats);
}
