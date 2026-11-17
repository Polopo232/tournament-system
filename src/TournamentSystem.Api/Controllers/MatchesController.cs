using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Security;

namespace TournamentSystem.Api.Controllers;

[ApiController]
[Route("api/matches")]
[Produces("application/json")]
public class MatchesController(IMatchService matchService) : ControllerBase
{
    [HttpPut("{id}/result")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(MatchResultResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchResultResponseDto>> SubmitResult(int id, SubmitMatchResultDto dto)
    {
        var actor = new CurrentUser(
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue(ClaimTypes.Role)!);

        var result = await matchService.SubmitResultAsync(id, dto, actor);
        return Ok(result);
    }
}