using TournamentSystem.Application.DTOs;

namespace TournamentSystem.Application.Interfaces;

public interface IUserService
{
    Task<UserPageDto> GetPagedAsync(UserQueryParams query);
    Task<UserDto> ChangeRoleAsync(int id, string role);
    Task<UserDto> SetBlockedAsync(int id, bool isBlocked, int actorId);
}