using System.ComponentModel.DataAnnotations;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Uue kasutaja registreerimise p‰ringu mudel.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Sisselogimise kasutajanimi (3ñ50 m‰rki).
    /// </summary>
    /// <example>nikita_dev</example>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Avalik nick, mida n‰evad teised m‰ngijad (3ñ20 m‰rki).
    /// </summary>
    /// <example>Nikita</example>
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string DisplayName { get; set; } = string.Empty;

    /// <example>nikita@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <example>SecretPass123!</example>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Kasutaja sisselogimise p‰ringu mudel.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Kasutajanimi.
    /// </summary>
    /// <example>nikita_dev</example>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Kasutaja parool.
    /// </summary>
    /// <example>SecretPass123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Autentimise vastuse mudel, mis sisaldab JWT tokenit ja kasutaja andmeid.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// JWT ligip‰‰sutoken edasiste p‰ringute autentimiseks.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Autenditud kasutaja profiili andmed.
    /// </summary>
    public UserDto User { get; set; } = new();
}