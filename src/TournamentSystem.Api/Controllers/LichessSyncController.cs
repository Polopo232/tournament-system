using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Lichessi arenade sünkroonimine. Käivitab Worker.
/// </summary>
[ApiController]
[Route("api/service/lichess")]
[Produces("application/json")]
[Authorize(Roles = "Service,Admin")]
public class LichessSyncController(
    ITournamentImportService importService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Sünkroonib arenad, mis vastavad seadistatud kategooriale ja kestusele.
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(LichessSyncResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LichessSyncResultDto>> Sync()
    {
        var perfKeys = configuration.GetSection("LichessSync:PerfKeys").Get<string[]>()
            ?? new[] { "rapid" };
        var minutes = configuration.GetValue<int?>("LichessSync:Minutes") ?? 10;

        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await importService.SyncLichessAsync(perfKeys, minutes, actorId);
        return Ok(result);
    }
}