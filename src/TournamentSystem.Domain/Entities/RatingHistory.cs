namespace TournamentSystem.Domain.Entities;

public class RatingHistory
{
    public int Id { get; set; }
    public string Game { get; set; } = string.Empty;
    public int OldRating { get; set; }
    public int NewRating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int PlayerId { get; set; }
    public User Player { get; set; } = null!;

    public int? MatchId { get; set; }
    public Match? Match { get; set; }
}