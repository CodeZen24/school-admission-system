using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Domain;
using SchoolAdmission.Api.Features.Classes;
using SchoolAdmission.Api.IntegrationTests.Fixtures;

namespace SchoolAdmission.Api.IntegrationTests;

[Collection(PostgreSqlDatabaseTestGroup.Name)]
public sealed class ClassApiTests(PostgreSqlDatabaseFixture fixture)
{
    private readonly PostgreSqlDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task CreateClassWithValidPayloadReturns201CreatedAndLocationHeader()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CreateClassRequest("Class 6 - Morning Shift", 40);

        using var response = await client.PostAsJsonAsync("/classes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var classResponse = await response.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(classResponse);
        Assert.NotEqual(Guid.Empty, classResponse.Id);
        Assert.Equal("Class 6 - Morning Shift", classResponse.Name);
        Assert.Equal(40, classResponse.TotalSeats);
        Assert.Equal($"/classes/{classResponse.Id}", response.Headers.Location.AbsolutePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateClassWithBlankNameReturns400BadRequestProblemDetails(string blankName)
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CreateClassRequest(blankName, 30);

        using var response = await client.PostAsJsonAsync("/classes", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateClassRequest.Name)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateClassWithNonPositiveTotalSeatsReturns400BadRequestProblemDetails(int invalidSeats)
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new CreateClassRequest("Class 7 - Day Shift", invalidSeats);

        using var response = await client.PostAsJsonAsync("/classes", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.True(problem.Errors.ContainsKey(nameof(CreateClassRequest.TotalSeats)));
    }

    [Fact]
    public async Task GetAllClassesReturns200WithDynamicSeatCounts()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        var createRequest = new CreateClassRequest("Class 9 - Dynamic Counts", 10);
        using var createResponse = await client.PostAsJsonAsync("/classes", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(created);

        await using (var db = _fixture.CreateDbContext())
        {
            db.Holds.AddRange(
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "11111111111111111",
                    Status = HoldStatus.Held,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "22222222222222222",
                    Status = HoldStatus.Held,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "33333333333333333",
                    Status = HoldStatus.Confirmed,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    ConfirmedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "44444444444444444",
                    Status = HoldStatus.Held,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-15),
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "55555555555555555",
                    Status = HoldStatus.Cancelled,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    CancelledAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

            await db.SaveChangesAsync();
        }

        using var listResponse = await client.GetAsync("/classes");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var classes = await listResponse.Content.ReadFromJsonAsync<List<ClassDetailsResponse>>();
        Assert.NotNull(classes);

        var targetClass = classes.FirstOrDefault(c => c.Id == created.Id);
        Assert.NotNull(targetClass);
        Assert.Equal("Class 9 - Dynamic Counts", targetClass.Name);
        Assert.Equal(10, targetClass.TotalSeats);
        Assert.Equal(2, targetClass.HeldSeats);
        Assert.Equal(1, targetClass.ConfirmedSeats);
        Assert.Equal(7, targetClass.AvailableSeats);
    }

    [Fact]
    public async Task GetClassByIdReturns200WithDynamicSeatCounts()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        var createRequest = new CreateClassRequest("Class 10 - Single Details", 15);
        using var createResponse = await client.PostAsJsonAsync("/classes", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(created);

        await using (var db = _fixture.CreateDbContext())
        {
            db.Holds.AddRange(
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "66666666666666666",
                    Status = HoldStatus.Held,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new AdmissionHold
                {
                    Id = Guid.NewGuid(),
                    ClassId = created.Id,
                    StudentId = "77777777777777777",
                    Status = HoldStatus.Confirmed,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    CreatedAt = DateTimeOffset.UtcNow,
                    ConfirmedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

            await db.SaveChangesAsync();
        }

        using var response = await client.GetAsync($"/classes/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<ClassDetailsResponse>();
        Assert.NotNull(details);
        Assert.Equal(created.Id, details.Id);
        Assert.Equal("Class 10 - Single Details", details.Name);
        Assert.Equal(15, details.TotalSeats);
        Assert.Equal(1, details.HeldSeats);
        Assert.Equal(1, details.ConfirmedSeats);
        Assert.Equal(13, details.AvailableSeats);
    }

    [Fact]
    public async Task GetClassByIdWithUnknownIdReturns404NotFoundProblemDetails()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        var unknownId = Guid.NewGuid();

        using var response = await client.GetAsync($"/classes/{unknownId}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Class not found.", problem.Title);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task GetClassByIdWithMalformedIdReturns400BadRequestProblemDetails()
    {
        using var factory = _fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/classes/not-a-valid-guid");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }
}
