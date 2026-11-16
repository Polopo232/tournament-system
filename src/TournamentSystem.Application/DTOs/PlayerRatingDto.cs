using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Mängija reiting ühes mängus.
/// </summary>
public class PlayerRatingDto
{
    /// <example>Chess</example>
    public string Game { get; set; } = string.Empty;

    /// <example>1250</example>
    public int Rating { get; set; }

    public static PlayerRatingDto FromEntity(PlayerRating r)
    {
        return new PlayerRatingDto
        {
            Game = r.Game,
            Rating = r.Rating
        };
    }
}