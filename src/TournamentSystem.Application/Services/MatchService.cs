using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class MatchService(
    IMatchRepository matches,
    ITournamentRepository tournaments,
    ITournamentNotifier notifier,
    ILogger<MatchService> logger) : IMatchService
{
    public async Task<MatchResultResponseDto> SubmitResultAsync(int matchId, SubmitMatchResultDto dto)
    {
        var match = await matches.GetByIdAsync(matchId)
            ?? throw new NotFoundException($"ID-ga {matchId} vastet ei leitud.");

        if (match.Status == MatchStatus.Completed)
        {
            throw new ValidationException("Mängu tulemus on juba esitatud.");
        }

        if (match.Player2Id == null)
        {
            throw new ValidationException("Vaba vooru tulemust ei saa esitada.");
        }

        var tournament = await tournaments.GetByIdAsync(match.TournamentId)
            ?? throw new NotFoundException($"Turniiri ID-ga {match.TournamentId} ei leitud.");

        if (tournament.Format == TournamentFormat.SingleElimination && dto.Player1Score == dto.Player2Score)
        {
            throw new ValidationException("Ühe väljalangemise turniiridel ei ole viike lubatud.");
        }

        match.Player1Score = dto.Player1Score;
        match.Player2Score = dto.Player2Score;
        match.Status = MatchStatus.Completed;
        match.PlayedAt = DateTime.UtcNow;

        if (dto.Player1Score > dto.Player2Score)
        {
            match.WinnerId = match.Player1Id;
        }
        else if (dto.Player2Score > dto.Player1Score)
        {
            match.WinnerId = match.Player2Id;
        }
        else
        {
            match.WinnerId = null;
        }

        await matches.SaveChangesAsync();

        var updatedTournament = await tournaments.GetByIdAsync(match.TournamentId);
        bool isRoundCompleted = updatedTournament!.Matches
            .Where(m => m.Round == updatedTournament.CurrentRound)
            .All(m => m.Status == MatchStatus.Completed);

        var matchDto = MatchDto.FromEntity(match);

        await notifier.NotifyMatchResultSavedAsync(match.TournamentId, matchDto);

        logger.LogInformation(
            "Mängu {MatchId} tulemus salvestatud ({Score1}:{Score2}). Voor lõppenud: {IsRoundCompleted}",
            matchId, dto.Player1Score, dto.Player2Score, isRoundCompleted);

        return new MatchResultResponseDto
        {
            Match = matchDto,
            IsRoundCompleted = isRoundCompleted
        };
    }
}