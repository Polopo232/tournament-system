namespace TournamentSystem.Domain.Entities;

public class Tournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TournamentFormat Format { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.Registration;
    public int MaxPlayers { get; set; }
    public int CurrentRound { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public int? WinnerId { get; set; }
    public User? Winner { get; set; }

    /// <summary>Välise allika ID, näiteks "lichess:abcd1234".</summary>
    public string? ExternalId { get; set; }

    /// <summary>Viimane sünkroonimise aeg (Lichess arenade jaoks).</summary>
    public DateTime? LastSyncedAt { get; set; }

    public List<Registration> Registrations { get; set; } = new();
    public List<Match> Matches { get; set; } = new();
    public List<ExternalDuel> Duels { get; set; } = new();
}