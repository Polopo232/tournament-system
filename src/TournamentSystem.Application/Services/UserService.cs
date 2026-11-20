using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class UserService(IUserRepository users, ILogger<UserService> logger) : IUserService
{
    public async Task<UserPageDto> GetPagedAsync(UserQueryParams query)
    {
        var (items, total) = await users.GetPagedAsync(query);

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        return new UserPageDto
        {
            Items = items.Select(UserDto.FromEntity).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = size,
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size))
        };
    }

    public async Task<UserDto> ChangeRoleAsync(int id, string role)
    {
        var user = await users.GetByIdAsync(id)
            ?? throw new NotFoundException($"User with ID {id} was not found.");

        if (!Enum.TryParse<UserRole>(role, true, out var parsed))
        {
            throw new ValidationException($"Unknown role: {role}.");
        }

        user.Role = parsed;
        await users.SaveChangesAsync();

        logger.LogInformation("Kasutaja (ID: {UserId}) uus roll on {Role}", id, parsed);
        return UserDto.FromEntity(user);
    }
}