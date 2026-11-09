using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Features.Classes;

namespace SchoolAdmission.Api.IntegrationTests;

/// <summary>
/// Shared helpers for tests that drive the real HTTP API against PostgreSQL.
/// </summary>
internal static class AdmissionApiHelpers
{
    public const string StudentIdHeader = "X-Student-Id";

    /// <summary>Builds a valid 17-digit student ID from a sequence number.</summary>
    public static string StudentId(int number) =>
        number.ToString(CultureInfo.InvariantCulture).PadLeft(17, '0');

    public static async Task<Guid> CreateClassAsync(HttpClient client, string name, int totalSeats)
    {
        using var response = await client.PostAsJsonAsync("/classes", new CreateClassRequest(name, totalSeats));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ClassResponse>();
        return created!.Id;
    }

    public static async Task<HttpResponseMessage> ApplyAsync(HttpClient client, Guid classId, string? studentId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        if (studentId is not null)
        {
            request.Headers.TryAddWithoutValidation(StudentIdHeader, studentId);
        }

        return await client.SendAsync(request);
    }

    public static async Task<HttpStatusCode> ApplyStatusAsync(HttpClient client, Guid classId, string studentId)
    {
        using var response = await ApplyAsync(client, classId, studentId);
        return response.StatusCode;
    }

    /// <summary>
    /// Starts every request only after all tasks exist, so they genuinely compete for the same rows.
    /// </summary>
    public static async Task<HttpStatusCode[]> RunConcurrentlyAsync(IEnumerable<Func<Task<HttpStatusCode>>> requests)
    {
        var gate = new TaskCompletionSource();
        var tasks = requests
            .Select(request => Task.Run(async () =>
            {
                await gate.Task;
                return await request();
            }))
            .ToList();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    public static async Task<ClassDetailsResponse> GetClassAsync(HttpClient client, Guid classId)
    {
        var details = await client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{classId}");
        return details!;
    }

    /// <summary>
    /// Counts classes whose live held plus confirmed seats exceed capacity. Must always be zero.
    /// </summary>
    public static async Task<int> CountOverCapacityClassesAsync(SchoolAdmissionDbContext db)
    {
        var rows = await db.Database
            .SqlQuery<int>($"""
                SELECT count(*)::int AS "Value"
                FROM admission_classes c
                WHERE (
                    SELECT count(*)
                    FROM admission_holds h
                    WHERE h.class_id = c.id
                      AND (h.status = 'CONFIRMED' OR (h.status = 'HELD' AND h.expires_at > now()))
                ) > c.total_seats
                """)
            .ToListAsync();

        return rows[0];
    }
}
