using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Mängu tulemuse sisestamise mudel.
/// </summary>
public class SubmitMatchResultDto
{
    /// <summary>
    /// Esimese mängija punktiskoor.
    /// </summary>
    /// <example>16</example>
    [Required]
    [Range(0, 10000, ErrorMessage = "Skoor ei tohi olla negatiivne.")]
    public int Player1Score { get; set; }

    /// <summary>
    /// Teise mängija punktiskoor.
    /// </summary>
    /// <example>12</example>
    [Required]
    [Range(0, 10000, ErrorMessage = "Skoor ei tohi olla negatiivne.")]
    public int Player2Score { get; set; }
}

/// <summary>
/// Mängu tulemuse salvestamise vastuse mudel.
/// </summary>
public class MatchResultResponseDto
{
    /// <summary>
    /// Uuendatud mängu andmed.
    /// </summary>
    public MatchDto Match { get; set; } = null!;

    /// <summary>
    /// Näitab, kas kõik praeguse vooru mängud on nüüd lõpetatud.
    /// </summary>
    /// <example>true</example>
    public bool IsRoundCompleted { get; set; }
}

/// <summary>
/// Lõppenud vooruga turniiri info taustateenuse (Worker) jaoks.
/// </summary>
public class CompletedRoundTournamentDto
{
    /// <summary>
    /// Turniiri unikaalne ID.
    /// </summary>
    /// <example>1</example>
    public int TournamentId { get; set; }

    /// <summary>
    /// Turniiri nimi.
    /// </summary>
    /// <example>Tallinn CS2 Cup 2026</example>
    public string TournamentName { get; set; } = string.Empty;

    /// <summary>
    /// Lõpetatud vooru number.
    /// </summary>
    /// <example>1</example>
    public int CompletedRound { get; set; }
}

/// <summary>
/// Mängija Elo-reitingu muudatuse kirje ajaloo jaoks.
/// </summary>
public class RatingHistoryDto
{
    /// <summary>
    /// Kirje unikaalne ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Mängu ID, mille järel reiting muutus.
    /// </summary>
    /// <example>5</example>
    public int? MatchId { get; set; }

    /// <summary>
    /// Reiting enne mängu.
    /// </summary>
    /// <example>1200</example>
    public int OldRating { get; set; }

    /// <summary>
    /// Reiting pärast mängu.
    /// </summary>
    /// <example>1216</example>
    public int NewRating { get; set; }

    /// <summary>
    /// Reitingu muutus (+/-).
    /// </summary>
    /// <example>16</example>
    public int RatingChange => NewRating - OldRating;

    /// <summary>
    /// Muudatuse kuupäev ja kellaaeg (UTC).
    /// </summary>
    /// <example>2026-11-13T12:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    public static RatingHistoryDto FromEntity(RatingHistory r)
    {
        return new RatingHistoryDto
        {
            Id = r.Id,
            MatchId = r.MatchId,
            OldRating = r.OldRating,
            NewRating = r.NewRating,
            CreatedAt = r.CreatedAt
        };
    }
}