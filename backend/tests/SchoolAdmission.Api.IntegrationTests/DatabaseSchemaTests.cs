using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.IntegrationTests.Fixtures;

namespace SchoolAdmission.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseTestGroup.Name)]
public sealed class DatabaseSchemaTests(PostgreSqlDatabaseFixture fixture)
{
    private readonly PostgreSqlDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task MigrationAppliesCleanlyOnFreshDatabase()
    {
        await using var context = _fixture.CreateDbContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        Assert.Empty(pendingMigrations);
        Assert.Contains(appliedMigrations, m => m.EndsWith("InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CanInsertAndQueryValidAdmissionClassAndHold()
    {
        await using var context = _fixture.CreateDbContext();

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "Class 6 - Morning Shift",
            TotalSeats = 40,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var hold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = "12345678901234567",
            Status = HoldStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Classes.Add(admissionClass);
        context.Holds.Add(hold);
        await context.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateDbContext();
        var savedClass = await verifyContext.Classes
            .Include(c => c.Holds)
            .FirstOrDefaultAsync(c => c.Id == admissionClass.Id);

        Assert.NotNull(savedClass);
        Assert.Equal("Class 6 - Morning Shift", savedClass.Name);
        Assert.Equal(40, savedClass.TotalSeats);
        Assert.Single(savedClass.Holds);
        Assert.Equal("12345678901234567", savedClass.Holds.First().StudentId);
        Assert.Equal(HoldStatus.Held, savedClass.Holds.First().Status);
    }

    [Fact]
    public async Task CheckConstraintRejectsNonPositiveTotalSeats()
    {
        await using var context = _fixture.CreateDbContext();

        var invalidClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "Class 7 - Day Shift",
            TotalSeats = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Classes.Add(invalidClass);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("ck_admission_classes_total_seats_positive", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task CheckConstraintRejectsEmptyClassName()
    {
        await using var context = _fixture.CreateDbContext();

        var invalidClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "   ",
            TotalSeats = 30,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Classes.Add(invalidClass);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("ck_admission_classes_name_nonempty", postgresEx.ConstraintName);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("123456789012345678")]
    [InlineData("1234567890123456A")]
    public async Task CheckConstraintRejectsInvalidStudentId(string invalidStudentId)
    {
        await using var context = _fixture.CreateDbContext();

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "Class 8 - Morning Shift",
            TotalSeats = 50,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Classes.Add(admissionClass);
        await context.SaveChangesAsync();

        var invalidHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = invalidStudentId,
            Status = HoldStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Holds.Add(invalidHold);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("ck_admission_holds_student_id_format", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task CheckConstraintRejectsInvalidStatus()
    {
        await using var context = _fixture.CreateDbContext();

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "Class 9 - Day Shift",
            TotalSeats = 25,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Classes.Add(admissionClass);
        await context.SaveChangesAsync();

        var invalidHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = "98765432109876543",
            Status = "PENDING",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Holds.Add(invalidHold);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("ck_admission_holds_status_valid", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task PartialUniqueIndexPreventsDuplicateActiveHoldsForSameClassAndStudent()
    {
        await using var context = _fixture.CreateDbContext();

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "Class 10 - Morning Shift",
            TotalSeats = 30,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Classes.Add(admissionClass);
        await context.SaveChangesAsync();

        const string studentId = "11122233344455566";

        var firstHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = studentId,
            Status = HoldStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Holds.Add(firstHold);
        await context.SaveChangesAsync();

        await using var duplicateContext = _fixture.CreateDbContext();
        var secondHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = studentId,
            Status = HoldStatus.Confirmed,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        duplicateContext.Holds.Add(secondHold);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("ix_admission_holds_class_id_student_id_active", postgresEx.ConstraintName);
    }

    [Fact]
    public async Task PartialUniqueIndexPermitsNewHoldAfterPreviousHoldCancelledOrExpired()
    {
        await using var context = _fixture.CreateDbContext();

        var admissionClass = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = "Class 5 - Day Shift",
            TotalSeats = 20,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Classes.Add(admissionClass);
        await context.SaveChangesAsync();

        const string studentId = "99988877766655544";

        var cancelledHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = studentId,
            Status = HoldStatus.Cancelled,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            CancelledAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Holds.Add(cancelledHold);
        await context.SaveChangesAsync();

        await using var secondContext = _fixture.CreateDbContext();
        var newHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = admissionClass.Id,
            StudentId = studentId,
            Status = HoldStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        secondContext.Holds.Add(newHold);
        await secondContext.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateDbContext();
        var studentHolds = await verifyContext.Holds
            .Where(h => h.ClassId == admissionClass.Id && h.StudentId == studentId)
            .ToListAsync();

        Assert.Equal(2, studentHolds.Count);
        Assert.Contains(studentHolds, h => h.Status == HoldStatus.Cancelled);
        Assert.Contains(studentHolds, h => h.Status == HoldStatus.Held);
    }

    [Fact]
    public async Task ForeignKeyConstraintPreventsHoldForNonExistentClass()
    {
        await using var context = _fixture.CreateDbContext();

        var orphanHold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = Guid.NewGuid(),
            StudentId = "55566677788899900",
            Status = HoldStatus.Held,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Holds.Add(orphanHold);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgresEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("FK_admission_holds_admission_classes_class_id", postgresEx.ConstraintName);
    }
}
