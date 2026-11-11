using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentService
{
    Task<PagedResultDto<TournamentDetailsDto>> GetAllAsync(TournamentQueryParams query);
    Task<TournamentDetailsDto> GetByIdAsync(int id);
    Task<TournamentDetailsDto> CreateAsync(CreateTournamentDto dto, int organizerId);
    Task<TournamentDetailsDto> UpdateAsync(int id, UpdateTournamentDto dto);
    Task DeleteAsync(int id);
}