using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Administraatori tööriistad välisandmete importimiseks.
/// </summary>
[ApiController]
[Route("api/admin/import")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class AdminImportController(ITournamentImportService importService) : ControllerBase
{
    /// <summary>
    /// Impordib lõppenud Lichessi arena tulemused. Sama arena teist korda ei impordita.
    /// </summary>
    /// <param name="arenaId">Lichessi arena ID, 8 märki (näiteks abcd1234).</param>
    [HttpPost("lichess/{arenaId}")]
    [ProducesResponseType(typeof(TournamentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDetailsDto>> ImportLichessArena(string arenaId)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tournament = await importService.ImportLichessArenaAsync(arenaId, adminId);
        return Ok(tournament);
    }
}