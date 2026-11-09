namespace TournamentSystem.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Player;
    public int Rating { get; set; } = 1200;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Tournament> OrganizedTournaments { get; set; } = new();
    public List<Registration> Registrations { get; set; } = new();
    public List<RatingHistory> RatingHistory { get; set; } = new();
}
