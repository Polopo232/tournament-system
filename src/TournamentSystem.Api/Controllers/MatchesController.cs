using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Mängude tulemuste haldamise lõpppunktid.
/// </summary>
[ApiController]
[Route("api/matches")]
[Produces("application/json")]
public class MatchesController(IMatchService matchService) : ControllerBase
{
    /// <summary>
    /// Salvestab mängu tulemuse, määrab võitja ja kontrollib vooru lõppemist.
    /// </summary>
    /// <param name="id">Mängu unikaalne ID.</param>
    /// <param name="dto">Mängijate skoorid.</param>
    /// <response code="200">Tulemus edukalt salvestatud.</response>
    /// <response code="400">Valideerimise viga (nt mäng on juba lõpetatud või viik keelatud).</response>
    /// <response code="404">Mängu ei leitud.</response>
    [HttpPut("{id}/result")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchResultResponseDto>> SubmitResult(int id, SubmitMatchResultDto dto)
    {
        var result = await matchService.SubmitResultAsync(id, dto);
        return Ok(result);
    }
}