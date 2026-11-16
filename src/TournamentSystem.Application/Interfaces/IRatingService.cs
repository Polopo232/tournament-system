using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface IRatingService
{
    Task<List<CompletedRoundTournamentDto>> GetCompletedRoundsTournamentsAsync();
    Task RecalculateTournamentRatingsAsync(int tournamentId);
    Task<List<RatingHistoryDto>> GetPlayerRatingHistoryAsync(int playerId);
    Task<List<PlayerRatingDto>> GetPlayerRatingsAsync(int playerId);
}