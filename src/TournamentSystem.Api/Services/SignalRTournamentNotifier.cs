using Microsoft.AspNetCore.SignalR;
using TournamentSystem.Api.Hubs;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Services;

public class SignalRTournamentNotifier(
    IHubContext<TournamentHub> hubContext,
    ILogger<SignalRTournamentNotifier> logger) : ITournamentNotifier
{
    public async Task NotifyMatchResultSavedAsync(int tournamentId, MatchDto match)
    {
        string groupName = TournamentHub.GetGroupName(tournamentId);
        await hubContext.Clients.Group(groupName).SendAsync("MatchResultSaved", new
        {
            TournamentId = tournamentId,
            Match = match
        });

        logger.LogInformation("Saadetud sündmus MatchResultSaved gruppi {GroupName} (MatchId: {MatchId}).", groupName, match.Id);
    }

    public async Task NotifyRoundCreatedAsync(int tournamentId, int round, List<MatchDto> matches)
    {
        string groupName = TournamentHub.GetGroupName(tournamentId);
        await hubContext.Clients.Group(groupName).SendAsync("RoundCreated", new
        {
            TournamentId = tournamentId,
            Round = round,
            Matches = matches
        });

        logger.LogInformation("Saadetud sündmus RoundCreated gruppi {GroupName} (Voor: {Round}).", groupName, round);
    }
}