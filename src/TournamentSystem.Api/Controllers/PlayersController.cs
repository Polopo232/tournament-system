using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Mängijate profiilid, reitingud ja edetabel.
/// </summary>
[ApiController]
[Route("api/players")]
[Produces("application/json")]
public class PlayersController(IRatingService ratingService) : ControllerBase
{
    /// <summary>
    /// Mängija avalik profiil: nick ja reitingud kõigis mängudes.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlayerProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerProfileDto>> GetProfile(int id)
    {
        var profile = await ratingService.GetPlayerProfileAsync(id);
        return Ok(profile);
    }

    /// <summary>
    /// Tagastab mängija Elo ajaloo kronoloogilises järjekorras.
    /// </summary>
    [HttpGet("{id:int}/rating-history")]
    [ProducesResponseType(typeof(List<RatingHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RatingHistoryDto>>> GetRatingHistory(int id)
    {
        var history = await ratingService.GetPlayerRatingHistoryAsync(id);
        return Ok(history);
    }

    /// <summary>
    /// Tagastab mängija reitingud kõigis mängudes.
    /// </summary>
    [HttpGet("{id:int}/ratings")]
    [ProducesResponseType(typeof(List<PlayerRatingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlayerRatingDto>>> GetRatings(int id)
    {
        var ratings = await ratingService.GetPlayerRatingsAsync(id);
        return Ok(ratings);
    }

    /// <summary>
    /// Edetabel valitud mängu järgi. Otsing nicki järgi, lehekülgede kaupa.
    /// </summary>
    /// <param name="game">Mäng, näiteks "Chess (rapid)".</param>
    /// <param name="search">Osa nickist.</param>
    /// <param name="page">Lehekülg, alates 1.</param>
    /// <param name="pageSize">Ridade arv lehel, kuni 100.</param>
    [HttpGet("leaderboard")]
    [ProducesResponseType(typeof(LeaderboardPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaderboardPageDto>> GetLeaderboard(
        [FromQuery] string game = "Chess",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await ratingService.GetLeaderboardAsync(game, search, page, pageSize);
        return Ok(result);
    }
}