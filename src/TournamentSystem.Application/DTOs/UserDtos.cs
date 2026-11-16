using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Kasutaja profiili andmete mudel.
/// </summary>
public class UserDto
{
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Sisselogimise kasutajanimi.
    /// </summary>
    /// <example>player1</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Avalik nick, mida kuvatakse kõikjal.
    /// </summary>
    /// <example>Mari</example>
    public string DisplayName { get; set; } = string.Empty;

    /// <example>nikita@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <example>Player</example>
    public string Role { get; set; } = string.Empty;

    public static UserDto FromEntity(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }
}

/// <summary>
/// Kasutaja rolli muutmise päringu mudel.
/// </summary>
public class ChangeRoleDto
{
    /// <example>Admin</example>
    [Required]
    [EnumDataType(typeof(UserRole))]
    public string Role { get; set; } = string.Empty;
}