using TournamentSystem.Worker.Services;

namespace TournamentSystem.Worker;

public class Worker(
    TournamentApiClient apiClient,
    IConfiguration configuration,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int intervalMinutes = configuration.GetValue<int?>("ApiSettings:IntervalMinutes") ?? 5;
        logger.LogInformation("TournamentSystem.Worker käivitus. Kontrolli intervall: {Minutes} minutit.", intervalMinutes);

        await RunCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        await ProcessCompletedRoundsAsync(stoppingToken);
        await SyncLichessAsync(stoppingToken);
    }

    private async Task ProcessCompletedRoundsAsync(CancellationToken stoppingToken)
    {
        List<TournamentApiClient.CompletedRoundDto> completedTournaments;

        try
        {
            completedTournaments = await apiClient.GetCompletedRoundsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ei õnnestunud laadida lõppenud voorude nimekirja.");
            return;
        }

        if (completedTournaments.Count == 0)
        {
            logger.LogInformation("Lõppenud voorudega turniire ei leitud ({Time}).", DateTimeOffset.Now);
            return;
        }

        foreach (var item in completedTournaments)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                logger.LogInformation(
                    "Töötlen turniiri '{Name}' (ID: {Id}), lõppenud voor: {Round}...",
                    item.TournamentName, item.TournamentId, item.CompletedRound);

                await apiClient.RecalculateRatingsAsync(item.TournamentId, stoppingToken);
                await apiClient.GenerateNextRoundAsync(item.TournamentId, stoppingToken);

                logger.LogInformation("Turniir {Id}: Elo ja järgmine voor on töödeldud.", item.TournamentId);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Turniiri {Id} töötlemine ebaõnnestus. Proovitakse uuesti järgmisel käivitusel.",
                    item.TournamentId);
            }
        }
    }

    private async Task SyncLichessAsync(CancellationToken stoppingToken)
    {
        try
        {
            var summary = await apiClient.SyncLichessAsync(stoppingToken);
            if (summary != null)
            {
                logger.LogInformation(
                    "Lichess: {Created} uut, {Updated} uuendatud, {Finished} lõppenud, {Skipped} vahele jäetud, {Failed} viga.",
                    summary.Created, summary.Updated, summary.Finished, summary.Skipped, summary.Failed);
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