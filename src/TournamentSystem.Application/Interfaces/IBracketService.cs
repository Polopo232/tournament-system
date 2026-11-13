using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface IBracketService
{
    Task<List<MatchDto>> GenerateBracketAsync(int tournamentId);
    Task<List<MatchDto>> GenerateNextRoundAsync(int tournamentId);
}