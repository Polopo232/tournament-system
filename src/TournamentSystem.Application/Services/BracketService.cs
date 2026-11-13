using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class BracketService(
    ITournamentRepository tournaments,
    IMatchRepository matches,
    ITournamentNotifier notifier,
    ILogger<BracketService> logger) : IBracketService
{
    public async Task<List<MatchDto>> GenerateBracketAsync(int tournamentId)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament with ID {tournamentId} was not found.");

        if (tournament.Status != TournamentStatus.Registration)
        {
            throw new ValidationException("Bracket has already been generated or the tournament is finished.");
        }

        if (tournament.Registrations.Count < 2)
        {
            throw new ValidationException("At least 2 registered players are required to generate a bracket.");
        }

        var seededPlayers = tournament.Registrations
            .Select(r => r.Player)
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.Id)
            .ToList();

        var createdMatches = tournament.Format == TournamentFormat.SingleElimination
            ? CreateSingleEliminationFirstRound(tournament.Id, seededPlayers)
            : CreateRoundRobinRound(tournament.Id, seededPlayers, round: 1);

        foreach (var match in createdMatches)
        {
            matches.AddRange(createdMatches);
        }

        tournament.Status = TournamentStatus.InProgress;
        tournament.CurrentRound = 1;

        await matches.SaveChangesAsync();
        await tournaments.SaveChangesAsync();

        logger.LogInformation(
            "Turniiritabel (1. voor, formaat {Format}) genereeritud turniirile {TournamentId}. Mängude arv: {Count}",
            tournament.Format, tournamentId, createdMatches.Count);

        return createdMatches.Select(MatchDto.FromEntity).ToList();
    }

    public async Task<List<MatchDto>> GenerateNextRoundAsync(int tournamentId)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament with ID {tournamentId} was not found.");

        if (tournament.Status != TournamentStatus.InProgress)
        {
            throw new ValidationException("Tournament is not currently in progress.");
        }

        var currentRoundMatches = tournament.Matches
            .Where(m => m.Round == tournament.CurrentRound)
            .OrderBy(m => m.Id)
            .ToList();

        if (currentRoundMatches.Count == 0)
        {
            throw new ValidationException($"No matches found for round {tournament.CurrentRound}.");
        }

        if (currentRoundMatches.Any(m => m.Status != MatchStatus.Completed))
        {
            throw new ValidationException("All matches in the current round must be completed before generating the next round.");
        }

        if (tournament.Format == TournamentFormat.SingleElimination)
        {
            return await HandleNextSingleEliminationRoundAsync(tournament, currentRoundMatches);
        }

        return await HandleNextRoundRobinRoundAsync(tournament);
    }

    private async Task<List<MatchDto>> HandleNextSingleEliminationRoundAsync(
        Tournament tournament,
        List<Match> currentRoundMatches)
    {
        var winners = currentRoundMatches
            .Select(m => m.Winner ?? (m.WinnerId == m.Player1Id ? m.Player1 : m.Player2))
            .Where(w => w != null)
            .Cast<User>()
            .ToList();

        if (winners.Count <= 1)
        {
            tournament.Status = TournamentStatus.Finished;
            await tournaments.SaveChangesAsync();

            logger.LogInformation("Turniir (ID: {TournamentId}) on lõppenud! Võitja ID: {WinnerId}",
                tournament.Id, winners.FirstOrDefault()?.Id);

            return [];
        }

        var nextRound = tournament.CurrentRound + 1;
        var nextMatches = new List<Match>();

        for (int i = 0; i < winners.Count; i += 2)
        {
            var p1 = winners[i];
            var p2 = i + 1 < winners.Count ? winners[i + 1] : null;

            var match = new Match
            {
                TournamentId = tournament.Id,
                Round = nextRound,
                Player1Id = p1.Id,
                Player1 = p1,
                Player2Id = p2?.Id,
                Player2 = p2,
                Status = p2 == null ? MatchStatus.Completed : MatchStatus.Pending,
                WinnerId = p2 == null ? p1.Id : null,
                PlayedAt = p2 == null ? DateTime.UtcNow : null
            };

            matches.AddRange(nextMatches);
            nextMatches.Add(match);
        }

        tournament.CurrentRound = nextRound;
        await matches.SaveChangesAsync();
        await tournaments.SaveChangesAsync();

        logger.LogInformation("SingleElimination {Round}. voor genereeritud turniirile {TournamentId}", nextRound, tournament.Id);
        return nextMatches.Select(MatchDto.FromEntity).ToList();
    }

    private async Task<List<MatchDto>> HandleNextRoundRobinRoundAsync(Tournament tournament)
    {
        var seededPlayers = tournament.Registrations
            .Select(r => r.Player)
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.Id)
            .ToList();

        int totalParticipants = seededPlayers.Count % 2 == 0 ? seededPlayers.Count : seededPlayers.Count + 1;
        int totalRounds = totalParticipants - 1;

        if (tournament.CurrentRound >= totalRounds)
        {
            tournament.Status = TournamentStatus.Finished;
            await tournaments.SaveChangesAsync();

            logger.LogInformation("RoundRobin turniir (ID: {TournamentId}) on lõppenud!", tournament.Id);
            return [];
        }

        var nextRound = tournament.CurrentRound + 1;
        var nextMatches = CreateRoundRobinRound(tournament.Id, seededPlayers, nextRound);

        foreach (var match in nextMatches)
        {
            matches.AddRange(nextMatches);
        }

        tournament.CurrentRound = nextRound;
        await matches.SaveChangesAsync();
        await tournaments.SaveChangesAsync();

        logger.LogInformation("RoundRobin {Round}. voor genereeritud turniirile {TournamentId}", nextRound, tournament.Id);
        return nextMatches.Select(MatchDto.FromEntity).ToList();
    }

    private static List<Match> CreateSingleEliminationFirstRound(int tournamentId, List<User> seededPlayers)
    {
        var result = new List<Match>();
        int count = seededPlayers.Count;
        int pairsCount = (int)Math.Ceiling(count / 2.0);

        for (int i = 0; i < pairsCount; i++)
        {
            var p1 = seededPlayers[i];
            int opponentIndex = count - 1 - i;
            var p2 = opponentIndex > i ? seededPlayers[opponentIndex] : null;

            result.Add(new Match
            {
                TournamentId = tournamentId,
                Round = 1,
                Player1Id = p1.Id,
                Player1 = p1,
                Player2Id = p2?.Id,
                Player2 = p2,
                Status = p2 == null ? MatchStatus.Completed : MatchStatus.Pending,
                WinnerId = p2 == null ? p1.Id : null,
                PlayedAt = p2 == null ? DateTime.UtcNow : null
            });
        }

        return result;
    }

    private static List<Match> CreateRoundRobinRound(int tournamentId, List<User> seededPlayers, int round)
    {
        var participants = new List<User?>(seededPlayers);
        if (participants.Count % 2 != 0)
        {
            participants.Add(null);
        }

        int n = participants.Count;
        for (int r = 1; r < round; r++)
        {
            var last = participants[n - 1];
            for (int i = n - 1; i > 1; i--)
            {
                participants[i] = participants[i - 1];
            }
            participants[1] = last;
        }

        var result = new List<Match>();
        for (int i = 0; i < n / 2; i++)
        {
            var p1 = participants[i];
            var p2 = participants[n - 1 - i];

            if (p1 == null && p2 == null) continue;

            var realPlayer1 = p1 ?? p2!;
            var realPlayer2 = p1 != null ? p2 : null;

            result.Add(new Match
            {
                TournamentId = tournamentId,
                Round = round,
                Player1Id = realPlayer1.Id,
                Player1 = realPlayer1,
                Player2Id = realPlayer2?.Id,
                Player2 = realPlayer2,
                Status = realPlayer2 == null ? MatchStatus.Completed : MatchStatus.Pending,
                WinnerId = realPlayer2 == null ? realPlayer1.Id : null,
                PlayedAt = realPlayer2 == null ? DateTime.UtcNow : null
            });
        }

        return result;
    }
}