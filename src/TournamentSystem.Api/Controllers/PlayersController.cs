using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Mängijate profiili ja reitingute lõpp-punktid.
/// </summary>
[ApiController]
[Route("api/players")]
[Produces("application/json")]
public class PlayersController(IRatingService ratingService) : ControllerBase
{
    /// <summary>
    /// Tagastab mängija Elo ajaloo kronoloogilises järjekorras.
    /// </summary>
    [HttpGet("{id}/rating-history")]
    [ProducesResponseType(typeof(List<RatingHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RatingHistoryDto>>> GetRatingHistory(int id)
    {
        var history = await ratingService.GetPlayerRatingHistoryAsync(id);
        return Ok(history);
    }

    /// <summary>
    /// Tagastab mängija reitingud kõigis mängudes.
    /// </summary>
    [HttpGet("{id}/ratings")]
    [ProducesResponseType(typeof(List<PlayerRatingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlayerRatingDto>>> GetRatings(int id)
    {
        var ratings = await ratingService.GetPlayerRatingsAsync(id);
        return Ok(ratings);
    }
}