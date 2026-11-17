using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Security;

namespace TournamentSystem.Application.Interfaces;

public interface IRatingService
{
    Task<List<CompletedRoundTournamentDto>> GetCompletedRoundsTournamentsAsync();
    Task RecalculateTournamentRatingsAsync(int tournamentId, CurrentUser? actor = null);
    Task<List<RatingHistoryDto>> GetPlayerRatingHistoryAsync(int playerId);
    Task<List<PlayerRatingDto>> GetPlayerRatingsAsync(int playerId);
}