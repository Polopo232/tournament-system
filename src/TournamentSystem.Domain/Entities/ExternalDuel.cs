namespace TournamentSystem.Domain.Entities;

/// <summary>Käimasolev partii Lichessi arenas.</summary>
public class ExternalDuel
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public string GameId { get; set; } = string.Empty;
    public string WhiteName { get; set; } = string.Empty;
    public int WhiteRating { get; set; }
    public string BlackName { get; set; } = string.Empty;
    public int BlackRating { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}