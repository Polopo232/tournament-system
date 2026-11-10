using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Kasutaja profiili andmete mudel.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Kasutaja unikaalne ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Kasutajanimi.
    /// </summary>
    /// <example>nikita_dev</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Kasutaja e-posti aadress.
    /// </summary>
    /// <example>nikita@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Kasutaja roll süsteemis (nt Player, Organizer, Admin).
    /// </summary>
    /// <example>Player</example>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Mängija praegune ELO reiting.
    /// </summary>
    /// <example>1200</example>
    public int Rating { get; set; }

    /// <summary>
    /// Teisendab domeeni User objekti UserDto mudeliks.
    /// </summary>
    /// <param name="user">Domeeni kasutaja olem.</param>
    /// <returns>Kasutaja DTO.</returns>
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

/// <summary>
/// Kasutaja rolli muutmise päringu mudel.
/// </summary>
public class ChangeRoleDto
{
    /// <summary>
    /// Uus roll, mis määratakse kasutajale (nt Admin, Organizer, Player).
    /// </summary>
    /// <example>Admin</example>
    [Required]
    [EnumDataType(typeof(UserRole))]
    public string Role { get; set; } = string.Empty;
}