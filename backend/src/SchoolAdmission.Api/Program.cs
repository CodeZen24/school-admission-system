using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Features.Classes;
using SchoolAdmission.Api.Infrastructure.Errors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddDbContext<SchoolAdmissionDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IClassesService, ClassesService>();

builder.Services
    .AddOptions<AdmissionOptions>()
    .BindConfiguration(AdmissionOptions.SectionName)
    .PostConfigure(options =>
    {
        var configuredDuration = builder.Configuration["HOLD_DURATION_SECONDS"];
        if (configuredDuration is null)
        {
            return;
        }

        options.HoldDurationSeconds = int.TryParse(
            configuredDuration,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var holdDurationSeconds)
                ? holdDurationSeconds
                : 0;
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SchoolAdmissionDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
