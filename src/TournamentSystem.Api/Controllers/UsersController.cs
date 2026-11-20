using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Kasutajate haldamise lõpp-punktid (ainult administraatorile).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController(IUserService userService, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Tagastab kasutajad lehekülgede kaupa. Toetab otsingut, rolli- ja allikafiltrit ning sortimist.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserPageDto>> GetAll([FromQuery] UserQueryParams query)
    {
        var result = await userService.GetPagedAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Muudab valitud kasutaja rolli.
    /// </summary>
    [HttpPut("{id:int}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> ChangeRole(int id, ChangeRoleDto dto)
    {
        var user = await userService.ChangeRoleAsync(id, dto.Role);
        return Ok(user);
    }

    /// <summary>
    /// Blokeerib või avab kasutaja konto. Enda kontot blokeerida ei saa.
    /// </summary>
    [HttpPut("{id:int}/block")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> SetBlocked(int id, BlockUserDto dto)
    {
        if (dto.Duration == "Minute1" && !env.IsDevelopment())
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid ban duration",
                Detail = "The one-minute test duration is available only in Development."
            });
        }

        var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await userService.SetBlockedAsync(id, dto, actorId);
        return Ok(user);
    }
}