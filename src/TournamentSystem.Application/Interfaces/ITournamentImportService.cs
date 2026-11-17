using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentImportService
{
    Task<TournamentDetailsDto> ImportLichessArenaAsync(string arenaId, int adminId);
    Task<LichessSyncResultDto> SyncLichessAsync(IReadOnlyList<string> perfKeys, int minutes, int actorId);
}