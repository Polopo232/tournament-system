using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class AuthService(IUserRepository users, ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await users.GetByUsernameAsync(dto.Username) != null || await users.GetByEmailAsync(dto.Email) != null)
        {
            return null;
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

        return CreateResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await users.GetByUsernameAsync(dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return null;
        }

        return CreateResponse(user);
    }

    public async Task<UserDto?> GetUserAsync(int id)
    {
        var user = await users.GetByIdAsync(id);
        return user == null ? null : UserDto.FromEntity(user);
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
