using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Security;

namespace TournamentSystem.Application.Interfaces;

public interface IMatchService
{
    Task<MatchResultResponseDto> SubmitResultAsync(int matchId, SubmitMatchResultDto dto, CurrentUser? actor = null);
}