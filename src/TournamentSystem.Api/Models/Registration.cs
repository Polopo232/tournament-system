namespace TournamentSystem.Api.Models;

public class Registration
{
    public int Id { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public int PlayerId { get; set; }
    public User Player { get; set; } = null!;
}
