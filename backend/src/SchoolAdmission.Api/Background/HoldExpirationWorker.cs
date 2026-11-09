using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

namespace SchoolAdmission.Api.Background;

/// <summary>
/// Background worker that periodically materializes expired holds in PostgreSQL without requiring API traffic.
/// </summary>
public sealed partial class HoldExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<HoldExpirationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<HoldExpirationWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger);

        await ExpireStaleHoldsAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExpireStaleHoldsAsync(stoppingToken);
        }

        LogStopped(_logger);
    }

    private async Task ExpireStaleHoldsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SchoolAdmissionDbContext>();

            var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE admission_holds
                SET status = 'EXPIRED', updated_at = now()
                WHERE status = 'HELD' AND expires_at <= now()
                """,
                cancellationToken);

            if (affected > 0)
            {
                LogExpiredBatch(_logger, affected);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            LogExpirationError(_logger, ex);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Hold expiration worker started.")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Hold expiration worker stopped.")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Materialized {Count} expired hold(s).")]
    private static partial void LogExpiredBatch(ILogger logger, int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "An error occurred during hold expiration processing.")]
    private static partial void LogExpirationError(ILogger logger, Exception exception);
}
