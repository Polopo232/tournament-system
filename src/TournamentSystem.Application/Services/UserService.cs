using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class UserService(IUserRepository users, ILogger<UserService> logger) : IUserService
{
    public async Task<List<UserDto>> GetAllAsync()
    {
        var list = await users.GetAllAsync();
        return list.Select(UserDto.FromEntity).ToList();
    }

    public async Task<UserDto> ChangeRoleAsync(int id, string role)
    {
        var user = await users.GetByIdAsync(id)
            ?? throw new NotFoundException($"User with ID {id} was not found.");

        user.Role = Enum.Parse<UserRole>(role);
        await users.SaveChangesAsync();

        logger.LogInformation("Kasutaja (ID: {UserId}) uus roll on {Role}", id, role);
        return UserDto.FromEntity(user);
    }
}