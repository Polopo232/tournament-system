namespace TournamentSystem.Application.Interfaces;

/// <summary>Turniir välisest allikast (Lichess arena).</summary>
public record ExternalArena(string Id, string Name, DateTime StartsAt, bool Finished);

/// <summary>Ühe mängija tulemus välisest turniirist.</summary>
public record ExternalArenaResult(int Rank, string Username, int Score);

/// <summary>Välise turniiriallika liides. Realisatsioon on Infrastructure-kihis.</summary>
public interface IExternalArenaSource
{
    Task<ExternalArena?> GetArenaAsync(string arenaId, CancellationToken cancellationToken = default);
    Task<List<ExternalArenaResult>> GetResultsAsync(string arenaId, CancellationToken cancellationToken = default);
}