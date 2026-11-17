using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Security;
using static TournamentSystem.Application.DTOs.TournamentDetailsDto;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentService
{
    Task<PagedResultDto<TournamentDetailsDto>> GetAllAsync(TournamentQueryParams query);
    Task<TournamentDetailsDto> GetByIdAsync(int id);
    Task<TournamentDetailsDto> CreateAsync(CreateTournamentDto dto, int organizerId);
    Task<TournamentDetailsDto> UpdateAsync(int id, UpdateTournamentDto dto, CurrentUser? actor = null);
    Task DeleteAsync(int id, CurrentUser? actor = null);
    Task<RegistrationDto> RegisterPlayerAsync(int tournamentId, int playerId);
}