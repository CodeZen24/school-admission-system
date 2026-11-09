using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Api.IntegrationTests.Fixtures;

/// <summary>
/// Manages the lifecycle of an ephemeral PostgreSQL container and migrates its schema.
/// </summary>
public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("school_admission_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<SchoolAdmissionDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new SchoolAdmissionDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public SchoolAdmissionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SchoolAdmissionDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SchoolAdmissionDbContext(options);
    }

    public WebApplicationFactory<Program> CreateWebApplicationFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseEnvironment("Development");
        });
    }
}
