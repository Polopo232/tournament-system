using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class RatingService(
    ITournamentRepository tournaments,
    IUserRepository users,
    IRatingRepository ratings,
    IMatchRepository matches,
    EloCalculatorService eloCalculator,
    ILogger<RatingService> logger) : IRatingService
{
    public async Task<List<CompletedRoundTournamentDto>> GetCompletedRoundsTournamentsAsync()
    {
        var allTournaments = await tournaments.GetAllAsync();
        var result = new List<CompletedRoundTournamentDto>();

        foreach (var t in allTournaments.Where(t => t.Status == TournamentStatus.InProgress))
        {
            var fullTournament = await tournaments.GetByIdAsync(t.Id);
            if (fullTournament == null) continue;

            var currentRoundMatches = fullTournament.Matches
                .Where(m => m.Round == fullTournament.CurrentRound)
                .ToList();

            // Kõik vooru mängud on mängitud JA vähemalt ühel mängul pole Elo veel ümber arvutatud
            if (currentRoundMatches.Count > 0 &&
                currentRoundMatches.All(m => m.Status == MatchStatus.Completed) &&
                currentRoundMatches.Any(m => !m.RatingsUpdated && m.Player2Id != null))
            {
                result.Add(new CompletedRoundTournamentDto
                {
                    TournamentId = fullTournament.Id,
                    TournamentName = fullTournament.Name,
                    CompletedRound = fullTournament.CurrentRound
                });
            }
        }

        return result;
    }

    public async Task RecalculateTournamentRatingsAsync(int tournamentId)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament with ID {tournamentId} was not found.");

        var matchesToProcess = tournament.Matches
            .Where(m => m.Status == MatchStatus.Completed && !m.RatingsUpdated)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.Id)
            .ToList();

        foreach (var match in matchesToProcess)
        {
            if (match.Player2Id == null)
            {
                match.RatingsUpdated = true;
                continue;
            }

            var p1 = await users.GetByIdAsync(match.Player1Id)
                ?? throw new NotFoundException($"Player {match.Player1Id} not found.");
            var p2 = await users.GetByIdAsync(match.Player2Id.Value)
                ?? throw new NotFoundException($"Player {match.Player2Id.Value} not found.");

            double p1Score = match.WinnerId == p1.Id ? 1.0 :
                             match.WinnerId == p2.Id ? 0.0 : 0.5;

            int oldRating1 = p1.Rating;
            int oldRating2 = p2.Rating;

            var (newRating1, newRating2) = eloCalculator.CalculateNewRatings(oldRating1, oldRating2, p1Score);

            p1.Rating = newRating1;
            p2.Rating = newRating2;

            ratings.Add(new RatingHistory
            {
                PlayerId = p1.Id,
                MatchId = match.Id,
                OldRating = oldRating1,
                NewRating = newRating1,
                CreatedAt = DateTime.UtcNow
            });

            ratings.Add(new RatingHistory
            {
                PlayerId = p2.Id,
                MatchId = match.Id,
                OldRating = oldRating2,
                NewRating = newRating2,
                CreatedAt = DateTime.UtcNow
            });

            match.RatingsUpdated = true;

            logger.LogInformation(
                "Elo-reiting uuendatud mängu {MatchId} järel: {P1} ({Old1} -> {New1}), {P2} ({Old2} -> {New2})",
                match.Id, p1.Username, oldRating1, newRating1, p2.Username, oldRating2, newRating2);
        }

        await matches.SaveChangesAsync();
        await users.SaveChangesAsync();
        await ratings.SaveChangesAsync();
    }

    public async Task<List<RatingHistoryDto>> GetPlayerRatingHistoryAsync(int playerId)
    {
        var player = await users.GetByIdAsync(playerId);
        if (player == null)
        {
            throw new NotFoundException($"Player with ID {playerId} was not found.");
        }

        var history = await ratings.GetByPlayerIdAsync(playerId);
        return history
            .OrderBy(h => h.CreatedAt)
            .Select(RatingHistoryDto.FromEntity)
            .ToList();
    }
}