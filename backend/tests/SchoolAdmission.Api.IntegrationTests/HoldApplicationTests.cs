using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Features.Holds;
using SchoolAdmission.Api.IntegrationTests.Fixtures;
using static SchoolAdmission.Api.IntegrationTests.AdmissionApiHelpers;

namespace SchoolAdmission.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseTestGroup.Name)]
public sealed class HoldApplicationTests(PostgreSqlDatabaseFixture fixture)
{
    private static readonly string[] ExpectedHoldFields = ["classId", "expiresAt", "holdId", "status", "studentId"];

    private readonly PostgreSqlDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ApplyReturns201WithExactContractAndDefaultDuration()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 1 - Contract", 5);

        var before = DateTimeOffset.UtcNow;
        using var response = await ApplyAsync(client, classId, StudentId(1));
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(ExpectedHoldFields, root.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.NotEqual(Guid.Empty, root.GetProperty("holdId").GetGuid());
        Assert.Equal(classId, root.GetProperty("classId").GetGuid());
        Assert.Equal(StudentId(1), root.GetProperty("studentId").GetString());
        Assert.Equal(HoldStatus.Held, root.GetProperty("status").GetString());

        var expiresAt = root.GetProperty("expiresAt").GetDateTimeOffset();
        Assert.InRange(expiresAt, before.AddSeconds(590), after.AddSeconds(610));
    }

    [Fact]
    public async Task ApplyHonoursConfiguredHoldDuration()
    {
        using var factory = _fixture.CreateWebApplicationFactory(holdDurationSeconds: 30);
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 2 - Short Hold", 5);

        var before = DateTimeOffset.UtcNow;
        using var response = await ApplyAsync(client, classId, StudentId(1));
        var after = DateTimeOffset.UtcNow;

        var hold = await response.Content.ReadFromJsonAsync<HoldResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(hold);
        Assert.InRange(hold.ExpiresAt, before.AddSeconds(25), after.AddSeconds(35));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567890123456")]
    [InlineData("123456789012345678")]
    [InlineData("1234567890123456A")]
    [InlineData("12345 67890123456")]
    public async Task ApplyWithMissingOrInvalidStudentIdReturns400ProblemDetails(string? studentId)
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 3 - Header", 5);

        using var response = await ApplyAsync(client, classId, studentId);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(StudentIdHeader));

        var details = await GetClassAsync(client, classId);
        Assert.Equal(0, details.HeldSeats);
    }

    [Theory]
    [InlineData("12345678901234567", true)]
    [InlineData("00000000000000000", true)]
    [InlineData("1234567890123456", false)]
    [InlineData("123456789012345678", false)]
    [InlineData("1234567890123456A", false)]
    [InlineData("١٢٣٤٥٦٧٨٩٠١٢٣٤٥٦٧", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void StudentIdentityAcceptsOnlySeventeenAsciiDigits(string? value, bool expected)
    {
        Assert.Equal(expected, StudentIdentity.IsValid(value));
    }

    [Fact]
    public async Task ApplyToUnknownClassReturns404ProblemDetails()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await ApplyAsync(client, Guid.NewGuid(), StudentId(1));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task ApplyToMalformedClassIdReturns400ProblemDetails()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/classes/not-a-guid/holds");
        request.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(1));
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SequentialApplicationsFillTheClassThenReturn409()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 4 - Sequential", 3);

        for (var n = 1; n <= 3; n++)
        {
            Assert.Equal(HttpStatusCode.Created, await ApplyStatusAsync(client, classId, StudentId(n)));
        }

        using var rejected = await ApplyAsync(client, classId, StudentId(4));
        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("The class has no available seats.", problem.Title);

        var details = await GetClassAsync(client, classId);
        Assert.Equal(3, details.HeldSeats);
        Assert.Equal(0, details.ConfirmedSeats);
        Assert.Equal(0, details.AvailableSeats);
    }

    [Fact]
    public async Task SecondApplicationFromSameStudentReturns409WithoutUsingASeat()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 5 - Duplicate", 5);

        Assert.Equal(HttpStatusCode.Created, await ApplyStatusAsync(client, classId, StudentId(1)));

        using var duplicate = await ApplyAsync(client, classId, StudentId(1));
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("The student already has an active or confirmed application for this class.", problem.Title);

        var details = await GetClassAsync(client, classId);
        Assert.Equal(1, details.HeldSeats);
        Assert.Equal(4, details.AvailableSeats);
    }

    [Fact]
    public async Task ConfirmedSeatBlocksSameStudentAndCountsTowardCapacity()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 6 - Confirmed", 2);
        await InsertHoldAsync(classId, StudentId(1), HoldStatus.Confirmed, DateTimeOffset.UtcNow.AddMinutes(10));

        Assert.Equal(HttpStatusCode.Conflict, await ApplyStatusAsync(client, classId, StudentId(1)));
        Assert.Equal(HttpStatusCode.Created, await ApplyStatusAsync(client, classId, StudentId(2)));
        Assert.Equal(HttpStatusCode.Conflict, await ApplyStatusAsync(client, classId, StudentId(3)));

        var details = await GetClassAsync(client, classId);
        Assert.Equal(1, details.HeldSeats);
        Assert.Equal(1, details.ConfirmedSeats);
        Assert.Equal(0, details.AvailableSeats);
    }

    [Fact]
    public async Task ExpiredHoldDoesNotConsumeCapacityAndIsMaterializedAsExpired()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 7 - Expired Capacity", 1);
        await InsertHoldAsync(classId, StudentId(1), HoldStatus.Held, DateTimeOffset.UtcNow.AddMinutes(-5));

        Assert.Equal(HttpStatusCode.Created, await ApplyStatusAsync(client, classId, StudentId(2)));

        Assert.Equal([HoldStatus.Expired], await GetStatusesAsync(classId, StudentId(1)));
        Assert.Equal([HoldStatus.Held], await GetStatusesAsync(classId, StudentId(2)));
    }

    [Fact]
    public async Task ExpiredHoldDoesNotBlockSameStudentFromApplyingAgain()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 8 - Reapply", 1);
        await InsertHoldAsync(classId, StudentId(1), HoldStatus.Held, DateTimeOffset.UtcNow.AddMinutes(-5));

        Assert.Equal(HttpStatusCode.Created, await ApplyStatusAsync(client, classId, StudentId(1)));

        var statuses = await GetStatusesAsync(classId, StudentId(1));
        Assert.Equal(2, statuses.Count);
        Assert.Contains(HoldStatus.Expired, statuses);
        Assert.Contains(HoldStatus.Held, statuses);
    }

    [Fact]
    public async Task RejectedApplicationStillPersistsExpiryMaterialization()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 9 - Reject Cleanup", 1);
        await InsertHoldAsync(classId, StudentId(1), HoldStatus.Held, DateTimeOffset.UtcNow.AddMinutes(10));
        await InsertHoldAsync(classId, StudentId(2), HoldStatus.Held, DateTimeOffset.UtcNow.AddMinutes(-5));

        Assert.Equal(HttpStatusCode.Conflict, await ApplyStatusAsync(client, classId, StudentId(3)));

        Assert.Equal([HoldStatus.Expired], await GetStatusesAsync(classId, StudentId(2)));
        Assert.Equal([HoldStatus.Held], await GetStatusesAsync(classId, StudentId(1)));
    }

    [Theory]
    [InlineData(1, 20)]
    [InlineData(10, 50)]
    [InlineData(25, 50)]
    public async Task ConcurrentApplicationsNeverOversell(int capacity, int requestCount)
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, $"Class 10 - Concurrent {capacity}/{requestCount}", capacity);

        var statuses = await RunConcurrentlyAsync(
            Enumerable.Range(1, requestCount)
                .Select(n => (Func<Task<HttpStatusCode>>)(() => ApplyStatusAsync(client, classId, StudentId(n)))));

        Assert.Equal(capacity, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(requestCount - capacity, statuses.Count(s => s == HttpStatusCode.Conflict));

        var details = await GetClassAsync(client, classId);
        Assert.Equal(capacity, details.HeldSeats);
        Assert.Equal(0, details.ConfirmedSeats);
        Assert.Equal(0, details.AvailableSeats);

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(capacity, await db.Holds.CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Held));
        Assert.Equal(0, await CountOverCapacityClassesAsync(db));
    }

    [Fact]
    public async Task ConcurrentApplicationsFromTheSameStudentCreateExactlyOneHold()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class 11 - Same Student Race", 10);

        var statuses = await RunConcurrentlyAsync(
            Enumerable.Range(1, 20)
                .Select(_ => (Func<Task<HttpStatusCode>>)(() => ApplyStatusAsync(client, classId, StudentId(1)))));

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(19, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal([HoldStatus.Held], await GetStatusesAsync(classId, StudentId(1)));
        Assert.Equal(9, (await GetClassAsync(client, classId)).AvailableSeats);
    }

    [Fact]
    public async Task ConcurrentApplicationsToDifferentClassesAllocateIndependently()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var firstClassId = await CreateClassAsync(client, "Class 12 - Parallel A", 5);
        var secondClassId = await CreateClassAsync(client, "Class 12 - Parallel B", 5);

        // The same 20 students apply to both classes: uniqueness is per class, capacity is per class.
        var targets = Enumerable.Range(1, 20)
            .SelectMany(n => new[] { (ClassId: firstClassId, Student: n), (ClassId: secondClassId, Student: n) })
            .ToList();

        var statuses = await RunConcurrentlyAsync(
            targets.Select(t => (Func<Task<HttpStatusCode>>)(() => ApplyStatusAsync(client, t.ClassId, StudentId(t.Student)))));

        foreach (var classId in new[] { firstClassId, secondClassId })
        {
            var created = targets.Zip(statuses).Count(x => x.First.ClassId == classId && x.Second == HttpStatusCode.Created);
            Assert.Equal(5, created);
            Assert.Equal(0, (await GetClassAsync(client, classId)).AvailableSeats);
        }

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(0, await CountOverCapacityClassesAsync(db));
    }

    private async Task InsertHoldAsync(Guid classId, string studentId, string status, DateTimeOffset expiresAt)
    {
        await using var db = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var createdAt = expiresAt > now ? now : expiresAt.AddMinutes(-10);
        db.Holds.Add(new AdmissionHold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId,
            Status = status,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            ConfirmedAt = status == HoldStatus.Confirmed ? now : null,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    private async Task<List<string>> GetStatusesAsync(Guid classId, string studentId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Holds
            .AsNoTracking()
            .Where(h => h.ClassId == classId && h.StudentId == studentId)
            .Select(h => h.Status)
            .ToListAsync();
    }
}
