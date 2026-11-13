using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TournamentSystem.Api.Hubs;

[Authorize]
public class TournamentHub(ILogger<TournamentHub> logger) : Hub
{
    public static string GetGroupName(int tournamentId) => $"tournament-{tournamentId}";

    /// <summary>
    /// Tellib reaalajas uuendused konkreetse turniiri kohta.
    /// </summary>
    public async Task JoinTournament(int tournamentId)
    {
        string groupName = GetGroupName(tournamentId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        logger.LogInformation("Klient {ConnectionId} liitus grupiga {GroupName}.", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Loobub konkreetse turniiri uuendustest.
    /// </summary>
    public async Task LeaveTournament(int tournamentId)
    {
        string groupName = GetGroupName(tournamentId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        logger.LogInformation("Klient {ConnectionId} lahkus grupist {GroupName}.", Context.ConnectionId, groupName);
    }
}