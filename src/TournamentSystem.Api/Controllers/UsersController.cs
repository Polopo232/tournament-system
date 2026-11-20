using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Tagastab kasutajad lehekülgede kaupa. Toetab otsingut, rolli- ja allikafiltrit ning sortimist.
    /// </summary>
    /// <param name="query">
    /// search: otsing nime, logini või e-posti järgi;
    /// role: Admin, Organizer, Player, Service;
    /// source: local või lichess;
    /// sortBy: name, newest, role;
    /// page, pageSize: lehekülg ja ridade arv (kuni 100).
    /// </param>
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
}