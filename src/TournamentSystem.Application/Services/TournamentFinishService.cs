using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Security;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class TournamentFinishService(
    ITournamentRepository tournaments,
    IRatingService ratings,
    ILogger<TournamentFinishService> logger) : ITournamentFinishService
{
    public async Task<TournamentDetailsDto> FinishAsync(int tournamentId, CurrentUser actor)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament with ID {tournamentId} was not found.");

        OwnershipRules.EnsureCanManage(actor, tournament);

        if (tournament.Status == TournamentStatus.Finished)
        {
            throw new ValidationException("Tournament is already finished.");
        }

        if (tournament.Status != TournamentStatus.InProgress)
        {
            throw new ValidationException("Tournament has not started yet.");
        }

        if (tournament.Matches.Any(m => m.Status != MatchStatus.Completed))
        {
            throw new ValidationException("All matches must be completed before the tournament can be finished.");
        }

        User winner = tournament.Format switch
        {
            TournamentFormat.SingleElimination => GetSingleEliminationChampion(tournament),
            TournamentFormat.RoundRobin => GetRoundRobinChampion(tournament),
            _ => throw new ValidationException("This tournament format cannot be finished manually.")
        };

        // Elo peab olema arvutatud enne lõpetamist, muidu viimase vooru mängud jäävad arvestamata.
        await ratings.RecalculateTournamentRatingsAsync(tournamentId, actor);

        tournament.Status = TournamentStatus.Finished;
        tournament.WinnerId = winner.Id;
        tournament.Winner = winner;

        await tournaments.SaveChangesAsync();

        logger.LogInformation("Turniir {TournamentId} lõpetati käsitsi. Võitja: {WinnerId}, kasutaja {ActorId}.",
            tournamentId, winner.Id, actor.Id);

        return TournamentDetailsDto.FromEntity(tournament);
    }

    private static User GetSingleEliminationChampion(Tournament tournament)
    {
        var currentRoundMatches = tournament.Matches
            .Where(m => m.Round == tournament.CurrentRound)
            .ToList();

        if (currentRoundMatches.Count != 1)
        {
            throw new ValidationException("The tournament is not in its final round yet.");
        }

        return TournamentResults.GetMatchWinner(currentRoundMatches[0])
            ?? throw new ValidationException("The final match has no winner.");
    }

    private static User GetRoundRobinChampion(Tournament tournament)
    {
        int participants = tournament.Registrations.Count;
        int totalParticipants = participants % 2 == 0 ? participants : participants + 1;
        int totalRounds = totalParticipants - 1;

        if (tournament.CurrentRound < totalRounds)
        {
            throw new ValidationException("Not all rounds have been played yet.");
        }

        return TournamentResults.DetermineRoundRobinWinner(tournament)
            ?? throw new ValidationException("Could not determine the winner.");
    }
}