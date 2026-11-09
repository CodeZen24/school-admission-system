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
public sealed class HoldLifecycleTests(PostgreSqlDatabaseFixture fixture)
{
    private readonly PostgreSqlDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task GetHoldReturns200WithExactFieldsAndEffectiveStatus()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L1 - GetHold", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(1));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        using var response = await client.GetAsync($"/holds/{hold.HoldId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(hold.HoldId, root.GetProperty("holdId").GetGuid());
        Assert.Equal(classId, root.GetProperty("classId").GetGuid());
        Assert.Equal(HoldStatus.Held, root.GetProperty("status").GetString());
        Assert.Equal(hold.ExpiresAt, root.GetProperty("expiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetHoldProjectsExpiredWhenPastDueEvenIfWorkerHasNotRun()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L2 - PastDue", 5);

        var holdId = Guid.NewGuid();
        await using (var db = _fixture.CreateDbContext())
        {
            db.Holds.Add(new AdmissionHold
            {
                Id = holdId,
                ClassId = classId,
                StudentId = StudentId(2),
                Status = HoldStatus.Held,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-20),
                UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-20)
            });
            await db.SaveChangesAsync();
        }

        using var response = await client.GetAsync($"/holds/{holdId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<HoldDetailsResponse>();
        Assert.NotNull(details);
        Assert.Equal(HoldStatus.Expired, details.Status);
    }

    [Fact]
    public async Task GetHoldWithUnknownIdReturns404ProblemDetails()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/holds/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task GetHoldWithMalformedIdReturns400ProblemDetails()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/holds/not-a-valid-guid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmHoldAsOwnerTransitionsToConfirmedAndIncrementsConfirmedSeats()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L3 - Confirm", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(3));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirmRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(3));
        using var response = await client.SendAsync(confirmRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmResult = await response.Content.ReadFromJsonAsync<ConfirmHoldResponse>();
        Assert.NotNull(confirmResult);
        Assert.Equal(hold.HoldId, confirmResult.HoldId);
        Assert.Equal(HoldStatus.Confirmed, confirmResult.Status);

        var classDetails = await GetClassAsync(client, classId);
        Assert.Equal(0, classDetails.HeldSeats);
        Assert.Equal(1, classDetails.ConfirmedSeats);
        Assert.Equal(4, classDetails.AvailableSeats);
    }

    [Fact]
    public async Task ConfirmHoldIsIdempotentAndDoesNotDoubleCount()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L4 - IdempotentConfirm", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(4));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        for (var i = 0; i < 2; i++)
        {
            using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
            confirmRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(4));
            using var response = await client.SendAsync(confirmRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var confirmResult = await response.Content.ReadFromJsonAsync<ConfirmHoldResponse>();
            Assert.NotNull(confirmResult);
            Assert.Equal(HoldStatus.Confirmed, confirmResult.Status);
        }

        var classDetails = await GetClassAsync(client, classId);
        Assert.Equal(0, classDetails.HeldSeats);
        Assert.Equal(1, classDetails.ConfirmedSeats);
        Assert.Equal(4, classDetails.AvailableSeats);
    }

    [Fact]
    public async Task ConfirmHoldByNonOwnerReturns403Forbidden()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L5 - NonOwnerConfirm", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(5));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        using var wrongStudentRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        wrongStudentRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(99));
        using var forbiddenResponse = await client.SendAsync(wrongStudentRequest);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        // Owner can still confirm afterward
        using var ownerRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        ownerRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(5));
        using var successResponse = await client.SendAsync(ownerRequest);

        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmExpiredHoldReturns409Conflict()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L6 - ConfirmExpired", 5);

        var holdId = Guid.NewGuid();
        await using (var db = _fixture.CreateDbContext())
        {
            db.Holds.Add(new AdmissionHold
            {
                Id = holdId,
                ClassId = classId,
                StudentId = StudentId(6),
                Status = HoldStatus.Held,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-15),
                UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-15)
            });
            await db.SaveChangesAsync();
        }

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdId}/confirm");
        confirmRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(6));
        using var response = await client.SendAsync(confirmRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelHoldAsOwnerReturns204AndReleasesCapacity()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L7 - Cancel", 2);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(7));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        var classBefore = await GetClassAsync(client, classId);
        Assert.Equal(1, classBefore.HeldSeats);
        Assert.Equal(1, classBefore.AvailableSeats);

        using var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        cancelRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(7));
        using var response = await client.SendAsync(cancelRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var classAfter = await GetClassAsync(client, classId);
        Assert.Equal(0, classAfter.HeldSeats);
        Assert.Equal(2, classAfter.AvailableSeats);
    }

    [Fact]
    public async Task CancelHoldByNonOwnerReturns403Forbidden()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L8 - NonOwnerCancel", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(8));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        wrongRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(88));
        using var forbiddenResponse = await client.SendAsync(wrongRequest);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task CancelConfirmedHoldReturns409Conflict()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L9 - CancelConfirmed", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(9));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirmRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(9));
        await client.SendAsync(confirmRequest);

        using var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        cancelRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(9));
        using var response = await client.SendAsync(cancelRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RepeatedCancellationIsIdempotent204()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L10 - IdempotentCancel", 5);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(10));
        var hold = await applyResponse.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        for (var i = 0; i < 2; i++)
        {
            using var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
            cancelRequest.Headers.TryAddWithoutValidation(StudentIdHeader, StudentId(10));
            using var response = await client.SendAsync(cancelRequest);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetMyHoldsReturnsStudentHistoryWithClassNamesOrderedNewestFirst()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        var studentId = StudentId(11);

        var classA = await CreateClassAsync(client, "Class History Alpha", 5);
        var classB = await CreateClassAsync(client, "Class History Beta", 5);

        using var applyA = await ApplyAsync(client, classA, studentId);
        var holdA = await applyA.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(holdA);

        using var confirmA = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdA.HoldId}/confirm");
        confirmA.Headers.TryAddWithoutValidation(StudentIdHeader, studentId);
        await client.SendAsync(confirmA);

        using var applyB = await ApplyAsync(client, classB, studentId);
        var holdB = await applyB.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(holdB);

        using var myHoldsRequest = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        myHoldsRequest.Headers.TryAddWithoutValidation(StudentIdHeader, studentId);
        using var response = await client.SendAsync(myHoldsRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<StudentHoldResponse>>();
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        // Newest first
        Assert.Equal(holdB.HoldId, list[0].HoldId);
        Assert.Equal("Class History Beta", list[0].ClassName);
        Assert.Equal(HoldStatus.Held, list[0].Status);
        Assert.NotEqual(default, list[0].CreatedAt);

        Assert.Equal(holdA.HoldId, list[1].HoldId);
        Assert.Equal("Class History Alpha", list[1].ClassName);
        Assert.Equal(HoldStatus.Confirmed, list[1].Status);
        Assert.NotEqual(default, list[1].CreatedAt);

        Assert.True(list[0].CreatedAt >= list[1].CreatedAt);
    }

    [Fact]
    public async Task AutomaticExpirationWorkerReleasesCapacityWithoutTraffic()
    {
        // Short hold duration (1 second) to test worker expiration
        using var factory = _fixture.CreateWebApplicationFactory(holdDurationSeconds: 1);
        using var client = factory.CreateClient();
        var classId = await CreateClassAsync(client, "Class L12 - AutoWorker", 1);

        using var applyResponse = await ApplyAsync(client, classId, StudentId(12));
        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);

        // Wait for hold to expire and background worker to process it (within 4 seconds)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var materialized = false;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(6))
        {
            await Task.Delay(500);
            await using var db = _fixture.CreateDbContext();
            var hold = await db.Holds.FirstOrDefaultAsync(h => h.ClassId == classId && h.StudentId == StudentId(12));
            if (hold is { Status: HoldStatus.Expired })
            {
                materialized = true;
                break;
            }
        }

        Assert.True(materialized, "The background worker did not materialize the expired hold within the timeout.");

        // A new student can now acquire the seat without any manual intervention
        using var newApplyResponse = await ApplyAsync(client, classId, StudentId(13));
        Assert.Equal(HttpStatusCode.Created, newApplyResponse.StatusCode);

        var classDetails = await GetClassAsync(client, classId);
        Assert.Equal(1, classDetails.HeldSeats);
        Assert.Equal(0, classDetails.AvailableSeats);
    }
}
