using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentNotifier
{
    Task NotifyMatchResultSavedAsync(int tournamentId, MatchDto match);
    Task NotifyRoundCreatedAsync(int tournamentId, int round, List<MatchDto> matches);


}