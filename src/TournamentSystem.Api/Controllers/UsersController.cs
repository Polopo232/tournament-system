using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        return Ok(await userService.GetAllAsync());
    }

    [HttpPut("{id}/role")]
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
