using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Security;
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

            if (currentRoundMatches.Count == 0) continue;

            // All matches in the current round must be played
            if (currentRoundMatches.Any(m => m.Status != MatchStatus.Completed)) continue;

            // The tournament needs to be processed before the next round is created.
            // This allows for a retry if the previous run crashed after the Elo recalculation.
            bool nextRoundExists = fullTournament.Matches
                .Any(m => m.Round == fullTournament.CurrentRound + 1);

            if (nextRoundExists) continue;

            result.Add(new CompletedRoundTournamentDto
            {
                TournamentId = fullTournament.Id,
                TournamentName = fullTournament.Name,
                CompletedRound = fullTournament.CurrentRound
            });
        }

        return result;
    }

    public async Task RecalculateTournamentRatingsAsync(int tournamentId, CurrentUser? actor = null)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament with ID {tournamentId} was not found.");
        OwnershipRules.EnsureCanManage(actor, tournament);

        var matchesToProcess = tournament.Matches
            .Where(m => m.Status == MatchStatus.Completed && !m.RatingsUpdated)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.Id)
            .ToList();

        if (matchesToProcess.Count == 0)
        {
            return;
        }

        var playerIds = matchesToProcess
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
            .OfType<int>()
            .ToList();

        var ratingMap = await ratings.GetOrCreateForPlayersAsync(playerIds, tournament.Game);

        foreach (var match in matchesToProcess)
        {
            if (match.Player2Id == null)
            {
                match.RatingsUpdated = true;
                continue;
            }

            int p1Id = match.Player1Id;
            int p2Id = match.Player2Id.Value;

            var r1 = ratingMap[p1Id];
            var r2 = ratingMap[p2Id];

            double p1Score = match.WinnerId == p1Id ? 1.0 :
                             match.WinnerId == p2Id ? 0.0 : 0.5;

            int old1 = r1.Rating;
            int old2 = r2.Rating;

            var (new1, new2) = eloCalculator.CalculateNewRatings(old1, old2, p1Score);

            r1.Rating = new1;
            r2.Rating = new2;

            ratings.Add(new RatingHistory
            {
                PlayerId = p1Id,
                MatchId = match.Id,
                Game = tournament.Game,
                OldRating = old1,
                NewRating = new1,
                CreatedAt = DateTime.UtcNow
            });

            ratings.Add(new RatingHistory
            {
                PlayerId = p2Id,
                MatchId = match.Id,
                Game = tournament.Game,
                OldRating = old2,
                NewRating = new2,
                CreatedAt = DateTime.UtcNow
            });

            match.RatingsUpdated = true;

            logger.LogInformation(
                "Elo uuendatud mängu {MatchId} järel ({Game}): {P1} ({Old1} -> {New1}), {P2} ({Old2} -> {New2})",
                match.Id, tournament.Game, p1Id, old1, new1, p2Id, old2, new2);
        }

        await ratings.SaveChangesAsync();
        await matches.SaveChangesAsync();
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

    public async Task<List<PlayerRatingDto>> GetPlayerRatingsAsync(int playerId)
    {
        var player = await users.GetByIdAsync(playerId);
        if (player == null)
        {
            throw new NotFoundException($"Player with ID {playerId} was not found.");
        }

        var list = await ratings.GetPlayerRatingsAsync(playerId);
        return list.Select(PlayerRatingDto.FromEntity).ToList();
    }

    public async Task<PlayerProfileDto> GetPlayerProfileAsync(int playerId)
    {
        var player = await users.GetByIdAsync(playerId)
            ?? throw new NotFoundException($"Player with ID {playerId} was not found.");

        var playerRatings = await ratings.GetPlayerRatingsAsync(playerId);

        return new PlayerProfileDto
        {
            Id = player.Id,
            DisplayName = player.DisplayName,
            Ratings = playerRatings.Select(PlayerRatingDto.FromEntity).ToList()
        };
    }

    public async Task<LeaderboardPageDto> GetLeaderboardAsync(string game, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await ratings.GetLeaderboardAsync(game, search, page, pageSize);
        int offset = (page - 1) * pageSize;

        return new LeaderboardPageDto
        {
            Items = items.Select((r, index) => new LeaderboardEntryDto
            {
                Rank = offset + index + 1,
                PlayerId = r.UserId,
                DisplayName = r.User.DisplayName,
                Game = r.Game,
                Rating = r.Rating
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))
        };
    }
}