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
    /// <response code="200">Kasutajate nimekiri edukalt tagastatud.</response>
    /// <response code="401">Kasutaja ei ole autenditud.</response>
    /// <response code="403">Puuduvad administraatori õigused.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        return Ok(await userService.GetAllAsync());
    }

    /// <summary>
    /// Muudab valitud kasutaja rolli süsteemis.
    /// </summary>
    /// <param name="id">Kasutaja unikaalne ID.</param>
    /// <param name="dto">Uus roll, mis kasutajale määratakse.</param>
    /// <response code="200">Kasutaja roll edukalt muudetud ja uuendatud andmed tagastatud.</response>
    /// <response code="401">Kasutaja ei ole autenditud.</response>
    /// <response code="403">Puuduvad administraatori õigused.</response>
    /// <response code="404">Sellise ID-ga kasutajat ei leitud.</response>
    [HttpPut("{id}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> ChangeRole(int id, ChangeRoleDto dto)
    {
        var user = await userService.ChangeRoleAsync(id, dto.Role);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}