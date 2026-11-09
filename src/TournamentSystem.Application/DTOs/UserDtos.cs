using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int Rating { get; set; }

    public static UserDto FromEntity(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            Rating = user.Rating
        };
    }
}

public class ChangeRoleDto
{
    [Required]
    [EnumDataType(typeof(UserRole))]
    public string Role { get; set; } = string.Empty;
}
