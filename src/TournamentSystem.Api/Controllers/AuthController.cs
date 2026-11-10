using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Kasutajate autentimise ja registreerimise lõpppunktid.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Registreerib uue kasutaja süsteemi.
    /// </summary>
    /// <param name="dto">Uue kasutaja registreerimise andmed.</param>
    /// <response code="201">Kasutaja edukalt loodud ja tagastatakse JWT token.</response>
    /// <response code="409">Kasutajanimi või e-posti aadress on juba kasutusel.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        if (result == null)
        {
            return Conflict("Username or email is already taken.");
        }

        return CreatedAtAction(nameof(Me), null, result);
    }

    /// <summary>
    /// Autendib kasutaja ja väljastab JWT tokeni.
    /// </summary>
    /// <param name="dto">Sisselogimise andmed (kasutajanimi/e-post ja parool).</param>
    /// <response code="200">Edukas sisselogimine, tagastatakse JWT token ja kasutaja info.</response>
    /// <response code="401">Vale kasutajanimi või parool.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        if (result == null)
        {
            return Unauthorized("Wrong username or password.");
        }

        return Ok(result);
    }

    /// <summary>
    /// Tagastab hetkel sisselogitud kasutaja profiili andmed.
    /// </summary>
    /// <response code="200">Kasutaja profiili andmed edukalt tagastatud.</response>
    /// <response code="401">Kasutaja ei ole autenditud (puudub kehtiv JWT token).</response>
    /// <response code="404">Kasutajat ei leitud.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await authService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}