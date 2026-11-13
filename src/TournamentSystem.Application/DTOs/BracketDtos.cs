using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Mängu (paari) andmete mudel turniiritabelis.
/// </summary>
public class MatchDto
{
    /// <summary>
    /// Mängu unikaalne ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Turniiri ID.
    /// </summary>
    /// <example>1</example>
    public int TournamentId { get; set; }

    /// <summary>
    /// Vooru number.
    /// </summary>
    /// <example>1</example>
    public int Round { get; set; }

    /// <summary>
    /// Esimese mängija ID.
    /// </summary>
    /// <example>2</example>
    public int Player1Id { get; set; }

    /// <summary>
    /// Esimese mängija kasutajanimi.
    /// </summary>
    /// <example>player1</example>
    public string Player1Username { get; set; } = string.Empty;

    /// <summary>
    /// Teise mängija ID (null, kui mängija saab vaba vooru ehk bye).
    /// </summary>
    /// <example>5</example>
    public int? Player2Id { get; set; }

    /// <summary>
    /// Teise mängija kasutajanimi.
    /// </summary>
    /// <example>player2</example>
    public string? Player2Username { get; set; }

    /// <summary>
    /// Esimese mängija skoor.
    /// </summary>
    /// <example>16</example>
    public int? Player1Score { get; set; }

    /// <summary>
    /// Teise mängija skoor.
    /// </summary>
    /// <example>10</example>
    public int? Player2Score { get; set; }

    /// <summary>
    /// Võitja mängija ID.
    /// </summary>
    /// <example>2</example>
    public int? WinnerId { get; set; }

    /// <summary>
    /// Mängu staatus (Pending või Completed).
    /// </summary>
    /// <example>Pending</example>
    public string Status { get; set; } = string.Empty;

    public static MatchDto FromEntity(Match m)
    {
        return new MatchDto
        {
            Id = m.Id,
            TournamentId = m.TournamentId,
            Round = m.Round,
            Player1Id = m.Player1Id,
            Player1Username = m.Player1?.Username ?? string.Empty,
            Player2Id = m.Player2Id,
            Player2Username = m.Player2?.Username,
            Player1Score = m.Player1Score,
            Player2Score = m.Player2Score,
            WinnerId = m.WinnerId,
            Status = m.Status.ToString()
        };
    }
}