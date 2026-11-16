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

        await ProcessCompletedRoundsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessCompletedRoundsAsync(stoppingToken);
        }
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

            // Iga turniir töödeldakse eraldi: viga ühes ei peata teisi.
            // Turniir jääb nimekirja, kuni järgmine voor on loodud, seega proovitakse uuesti järgmisel käivitusel.
            try
            {
                logger.LogInformation(
                    "Töötlen turniiri '{Name}' (ID: {Id}), lõppenud voor: {Round}...",
                    item.TournamentName, item.TournamentId, item.CompletedRound);

                await apiClient.RecalculateRatingsAsync(item.TournamentId, stoppingToken);
                logger.LogInformation("Elo-reitingud on turniiri {Id} jaoks ümber arvutatud.", item.TournamentId);

                await apiClient.GenerateNextRoundAsync(item.TournamentId, stoppingToken);
                logger.LogInformation("Järgmine voor on turniiri {Id} jaoks moodustatud.", item.TournamentId);
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
}