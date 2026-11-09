namespace TournamentSystem.Domain.Entities;

public class Match
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int? Player1Score { get; set; }
    public int? Player2Score { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public bool RatingsUpdated { get; set; }
    public DateTime? PlayedAt { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public int Player1Id { get; set; }
    public User Player1 { get; set; } = null!;

    public int? Player2Id { get; set; }
    public User? Player2 { get; set; }

    public int? WinnerId { get; set; }
    public User? Winner { get; set; }
}
