using TournamentSystem.Worker.Services;

namespace TournamentSystem.Worker;

public class LichessSyncWorker(
    TournamentApiClient apiClient,
    IConfiguration configuration,
    ILogger<LichessSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int seconds = configuration.GetValue<int?>("LichessSync:IntervalSeconds") ?? 60;
        logger.LogInformation("Lichessi sünkroon käivitub iga {Seconds} sekundi järel.", seconds);

        await SyncOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncOnceAsync(stoppingToken);
        }
    }

    private async Task SyncOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            var summary = await apiClient.SyncLichessAsync(stoppingToken);
            if (summary != null && summary.Created + summary.Updated + summary.Finished > 0)
            {
                logger.LogInformation(
                    "Lichess: {Created} uut, {Updated} uuendatud, {Finished} lõppenud, {Failed} viga.",
                    summary.Created, summary.Updated, summary.Finished, summary.Failed);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lichessi sünkroonimine ebaõnnestus.");
        }
    }
}