using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> ChangeRoleAsync(int id, string role);
}
