using System;
using System.Collections.Generic;
using System.Text;
using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces
{
    public interface IMatchService
    {
        Task<MatchResultResponseDto> SubmitResultAsync(int matchId, SubmitMatchResultDto dto);
    }
}
