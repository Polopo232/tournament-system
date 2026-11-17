namespace TournamentSystem.Application.Interfaces;

/// <summary>Turniir välisest allikast (Lichess arena).</summary>
public record ExternalArena(string Id, string Name, DateTime StartsAt, bool Finished);

/// <summary>Ühe mängija tulemus välisest turniirist.</summary>
public record ExternalArenaResult(int Rank, string Username, int Score);

/// <summary>Kokkuvõte arenast nimekirjas (GET /api/tournament).</summary>
public record ExternalArenaSummary(
    string Id,
    string Name,
    string PerfKey,
    int Minutes,
    int Status,
    DateTime StartsAt);

public interface IExternalArenaSource
{
    Task<ExternalArena?> GetArenaAsync(string arenaId, CancellationToken cancellationToken = default);
    Task<List<ExternalArenaResult>> GetResultsAsync(string arenaId, CancellationToken cancellationToken = default);
    Task<List<ExternalArenaSummary>> GetArenaListAsync(CancellationToken cancellationToken = default);
}