using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Mängijate statistika ja reitingu ajaloo lõpppunktid.
/// </summary>
[ApiController]
[Route("api/players")]
[Produces("application/json")]
public class PlayersController(IRatingService ratingService) : ControllerBase
{
    /// <summary>
    /// Tagastab mängija Elo-reitingu muudatuste ajaloo.
    /// </summary>
    /// <param name="id">Mängija unikaalne ID.</param>
    /// <response code="200">Reitingu ajalugu edukalt tagastatud.</response>
    /// <response code="404">Mängijat ei leitud.</response>
    [HttpGet("{id}/rating-history")]
    [ProducesResponseType(typeof(List<RatingHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RatingHistoryDto>>> GetRatingHistory(int id)
    {
        var history = await ratingService.GetPlayerRatingHistoryAsync(id);
        return Ok(history);
    }
}