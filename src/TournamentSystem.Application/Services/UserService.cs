using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class UserService(IUserRepository users) : IUserService
{
    public async Task<List<UserDto>> GetAllAsync()
    {
        var list = await users.GetAllAsync();
        return list.Select(UserDto.FromEntity).ToList();
    }

    public async Task<UserDto?> ChangeRoleAsync(int id, string role)
    {
        var user = await users.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        user.Role = Enum.Parse<UserRole>(role);
        await users.SaveChangesAsync();

        return UserDto.FromEntity(user);
    }
}
