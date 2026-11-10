using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Kasutajate haldamise lõpppunktid (ainult administraatorile).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Tagastab kõigi registreeritud kasutajate nimekirja.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        return Ok(await userService.GetAllAsync());
    }

    /// <summary>
    /// Muudab valitud kasutaja rolli süsteemis.
    /// </summary>
    [HttpPut("{id}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> ChangeRole(int id, ChangeRoleDto dto)
    {
        var user = await userService.ChangeRoleAsync(id, dto.Role);
        return Ok(user);
    }
}