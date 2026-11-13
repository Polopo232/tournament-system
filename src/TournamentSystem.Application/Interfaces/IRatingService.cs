using System;
using System.Collections.Generic;
using System.Text;
using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces
{
    public interface IRatingService
    {
        Task<List<CompletedRoundTournamentDto>> GetCompletedRoundsTournamentsAsync();
        Task RecalculateTournamentRatingsAsync(int tournamentId);
        Task<List<RatingHistoryDto>> GetPlayerRatingHistoryAsync(int playerId);
    }
}
