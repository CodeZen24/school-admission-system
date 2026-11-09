using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Data.Configurations;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Features.Holds;

public sealed class HoldsService(
    SchoolAdmissionDbContext dbContext,
    IOptions<AdmissionOptions> options) : IHoldsService
{
    private readonly SchoolAdmissionDbContext _dbContext = dbContext;
    private readonly int _holdDurationSeconds = options.Value.HoldDurationSeconds;

    /// <summary>
    /// Reserves one seat. Every allocation for a class is serialized by a row lock on that class,
    /// so correctness comes from PostgreSQL and holds across API processes, not from process memory.
    /// Lock order is always class row first, then hold rows.
    /// </summary>
    public async Task<ApplyHoldResult> ApplyAsync(
        Guid classId,
        string studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var totalSeatsRows = await _dbContext.Database
            .SqlQuery<int>($"""SELECT total_seats AS "Value" FROM admission_classes WHERE id = {classId} FOR UPDATE""")
            .ToListAsync(cancellationToken);

        if (totalSeatsRows.Count == 0)
        {
            return new ApplyHoldResult(ApplyHoldOutcome.ClassNotFound);
        }

        var totalSeats = totalSeatsRows[0];

        var now = await GetDatabaseClockTimeAsync(cancellationToken);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE admission_holds
            SET status = 'EXPIRED', updated_at = {now}
            WHERE class_id = {classId} AND status = 'HELD' AND expires_at <= {now}
            """,
            cancellationToken);

        var alreadyApplied = await _dbContext.Holds.AnyAsync(
            h => h.ClassId == classId
                && h.StudentId == studentId
                && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed),
            cancellationToken);

        if (alreadyApplied)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ApplyHoldResult(ApplyHoldOutcome.DuplicateApplication);
        }

        var occupiedSeats = await _dbContext.Holds.CountAsync(
            h => h.ClassId == classId
                && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed),
            cancellationToken);

        if (occupiedSeats >= totalSeats)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ApplyHoldResult(ApplyHoldOutcome.ClassFull);
        }

        var holdId = Guid.NewGuid();
        var expiresAt = now.AddSeconds(_holdDurationSeconds);

        try
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO admission_holds
                    (id, class_id, student_id, status, expires_at, created_at, updated_at)
                VALUES
                    ({holdId}, {classId}, {studentId}, 'HELD', {expiresAt}, {now}, {now})
                """,
                cancellationToken);
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation
            && exception.ConstraintName == AdmissionHoldConfiguration.ActiveApplicationIndexName)
        {
            return new ApplyHoldResult(ApplyHoldOutcome.DuplicateApplication);
        }

        var hold = await _dbContext.Holds
            .AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => new HoldResponse(h.Id, h.ClassId, h.StudentId, h.Status, h.ExpiresAt))
            .SingleAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ApplyHoldResult(ApplyHoldOutcome.Created, hold);
    }

    public async Task<HoldDetailsResponse?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken)
    {
        return await _dbContext.Holds
            .AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => new HoldDetailsResponse(
                h.Id,
                h.ClassId,
                h.Status == HoldStatus.Held && h.ExpiresAt <= DateTimeOffset.UtcNow
                    ? HoldStatus.Expired
                    : h.Status,
                h.ExpiresAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConfirmHoldResult> ConfirmAsync(
        Guid holdId,
        string studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var hold = await _dbContext.Holds
            .FromSqlInterpolated($"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        if (hold is null)
        {
            return new ConfirmHoldResult(ConfirmHoldOutcome.NotFound);
        }

        if (!string.Equals(hold.StudentId, studentId, StringComparison.Ordinal))
        {
            return new ConfirmHoldResult(ConfirmHoldOutcome.Forbidden);
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ConfirmHoldResult(
                ConfirmHoldOutcome.Confirmed,
                new ConfirmHoldResponse(hold.Id, HoldStatus.Confirmed));
        }

        if (hold.Status == HoldStatus.Cancelled)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ConfirmHoldResult(ConfirmHoldOutcome.Cancelled);
        }

        var now = await GetDatabaseClockTimeAsync(cancellationToken);
        if (hold.Status == HoldStatus.Expired || hold.ExpiresAt <= now)
        {
            if (hold.Status == HoldStatus.Held)
            {
                hold.Status = HoldStatus.Expired;
                hold.UpdatedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new ConfirmHoldResult(ConfirmHoldOutcome.Expired);
        }

        hold.Status = HoldStatus.Confirmed;
        hold.ConfirmedAt = now;
        hold.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ConfirmHoldResult(
            ConfirmHoldOutcome.Confirmed,
            new ConfirmHoldResponse(hold.Id, HoldStatus.Confirmed));
    }

    public async Task<CancelHoldResult> CancelAsync(
        Guid holdId,
        string studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var hold = await _dbContext.Holds
            .FromSqlInterpolated($"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        if (hold is null)
        {
            return new CancelHoldResult(CancelHoldOutcome.NotFound);
        }

        if (!string.Equals(hold.StudentId, studentId, StringComparison.Ordinal))
        {
            return new CancelHoldResult(CancelHoldOutcome.Forbidden);
        }

        if (hold.Status == HoldStatus.Confirmed)
        {
            await transaction.CommitAsync(cancellationToken);
            return new CancelHoldResult(CancelHoldOutcome.ConflictConfirmed);
        }

        if (hold.Status == HoldStatus.Cancelled || hold.Status == HoldStatus.Expired)
        {
            await transaction.CommitAsync(cancellationToken);
            return new CancelHoldResult(CancelHoldOutcome.Cancelled);
        }

        var now = await GetDatabaseClockTimeAsync(cancellationToken);
        if (hold.ExpiresAt <= now)
        {
            hold.Status = HoldStatus.Expired;
            hold.UpdatedAt = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CancelHoldResult(CancelHoldOutcome.Cancelled);
        }

        hold.Status = HoldStatus.Cancelled;
        hold.CancelledAt = now;
        hold.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CancelHoldResult(CancelHoldOutcome.Cancelled);
    }

    public async Task<IReadOnlyList<StudentHoldResponse>> GetMyHoldsAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new StudentHoldResponse(
                h.Id,
                h.ClassId,
                h.Class.Name,
                h.Status == HoldStatus.Held && h.ExpiresAt <= DateTimeOffset.UtcNow
                    ? HoldStatus.Expired
                    : h.Status,
                h.ExpiresAt,
                h.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the current PostgreSQL wall-clock timestamp using clock_timestamp() after acquiring
    /// the relevant row lock. This ensures lock queue wait times do not shorten hold duration
    /// or cause expiry checks to evaluate against stale transaction-start timestamps.
    /// </summary>
    private async Task<DateTimeOffset> GetDatabaseClockTimeAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Database
            .SqlQuery<DateTimeOffset>($"""SELECT clock_timestamp() AS "Value" """)
            .SingleAsync(cancellationToken);
    }
}
