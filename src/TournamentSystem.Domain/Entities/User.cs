using System.ComponentModel.DataAnnotations.Schema;

namespace TournamentSystem.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Player;

    /// <summary>Blokeerimise põhjus. Tühi, kui konto ei ole blokeeritud.</summary>
    public BanReason? BanReason { get; set; }

    /// <summary>Blokeeringu lõpp (UTC). Tühi tähendab püsivat blokeeringut.</summary>
    public DateTime? BlockedUntil { get; set; }

    /// <summary>Blokeerimise algus (UTC).</summary>
    public DateTime? BlockedAt { get; set; }

    /// <summary>Kas konto on praegu blokeeritud. Arvestab lõppaega, aegunud blokeering ei kehti.</summary>
    [NotMapped]
    public bool IsBlocked => BanReason != null && (BlockedUntil == null || BlockedUntil > DateTime.UtcNow);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Tournament> OrganizedTournaments { get; set; } = new();
    public List<Registration> Registrations { get; set; } = new();
    public List<RatingHistory> RatingHistory { get; set; } = new();
    public List<PlayerRating> Ratings { get; set; } = new();
}