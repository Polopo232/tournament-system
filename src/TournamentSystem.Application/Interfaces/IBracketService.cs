using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Security;

namespace TournamentSystem.Application.Interfaces;

public interface IBracketService
{
    Task<List<MatchDto>> GenerateBracketAsync(int tournamentId, CurrentUser? actor = null);
    Task<List<MatchDto>> GenerateNextRoundAsync(int tournamentId, CurrentUser? actor = null);
}