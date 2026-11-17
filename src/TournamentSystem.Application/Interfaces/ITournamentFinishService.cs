using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Security;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentFinishService
{
    Task<TournamentDetailsDto> FinishAsync(int tournamentId, CurrentUser actor);
}