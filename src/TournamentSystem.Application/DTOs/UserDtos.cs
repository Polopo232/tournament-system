using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>Kasutaja profiili andmete mudel.</summary>
public class UserDto
{
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>Sisselogimise kasutajanimi.</summary>
    /// <example>player1</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>Avalik nick.</summary>
    /// <example>Mari</example>
    public string DisplayName { get; set; } = string.Empty;

    /// <example>mari@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <example>Player</example>
    public string Role { get; set; } = string.Empty;

    /// <summary>Kas konto on blokeeritud.</summary>
    /// <example>false</example>
    public bool IsBlocked { get; set; }

    public static UserDto FromEntity(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsBlocked = user.IsBlocked
        };
    }
}

/// <summary>Kasutaja rolli muutmise päringu mudel.</summary>
public class ChangeRoleDto
{
    /// <example>Admin</example>
    [Required]
    [EnumDataType(typeof(UserRole))]
    public string Role { get; set; } = string.Empty;
}

/// <summary>Kasutaja blokeerimise või avamise päringu mudel.</summary>
public class BlockUserDto
{
    /// <summary>true blokeerib konto, false avab selle.</summary>
    /// <example>true</example>
    public bool IsBlocked { get; set; }
}