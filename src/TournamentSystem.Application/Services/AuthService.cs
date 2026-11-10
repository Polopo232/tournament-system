using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class AuthService(IUserRepository users, ITokenService tokenService, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await users.GetByUsernameAsync(dto.Username) != null || await users.GetByEmailAsync(dto.Email) != null)
        {
            throw new ValidationException("Username or email is already taken.");
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Player
        };

        users.Add(user);
        await users.SaveChangesAsync();

        logger.LogInformation("Uus kasutaja registreeritud: {Username}", dto.Username);
        return CreateResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await users.GetByUsernameAsync(dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            logger.LogWarning("Ebaõnnestunud sisselogimiskatse kasutajale: {Username}", dto.Username);
            throw new UnauthorizedAccessException("Wrong username or password.");
        }

        logger.LogInformation("Kasutaja {Username} logis edukalt sisse.", dto.Username);
        return CreateResponse(user);
    }

    public async Task<UserDto> GetUserAsync(int id)
    {
        var user = await users.GetByIdAsync(id)
            ?? throw new NotFoundException($"User with ID {id} was not found.");

        return UserDto.FromEntity(user);
    }

    private AuthResponseDto CreateResponse(User user)
    {
        return new AuthResponseDto
        {
            Token = tokenService.CreateToken(user),
            User = UserDto.FromEntity(user)
        };
    }
}