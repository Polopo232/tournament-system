using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentImportService
{
    Task<TournamentDetailsDto> ImportLichessArenaAsync(string arenaId, int adminId);
}