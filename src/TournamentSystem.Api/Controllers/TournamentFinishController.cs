using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Security;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Turniiri käsitsi lõpetamine.
/// </summary>
[ApiController]
[Route("api/tournaments")]
[Produces("application/json")]
[Authorize(Roles = "Organizer,Admin")]
public class TournamentFinishController(ITournamentFinishService finishService) : ControllerBase
{
    /// <summary>
    /// Lõpetab turniiri: arvutab Elo, määrab võitja ja muudab staatuse.
    /// Nõuab, et kõik mängud oleksid lõppenud.
    /// </summary>
    [HttpPost("{id:int}/finish")]
    [ProducesResponseType(typeof(TournamentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDetailsDto>> Finish(int id)
    {
        var actor = new CurrentUser(
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue(ClaimTypes.Role)!);

        var result = await finishService.FinishAsync(id, actor);
        return Ok(result);
    }
}