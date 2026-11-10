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
    /// <response code="400">Valideerimise viga (kasutajanimi/e-post on juba kasutusel).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var result = await authService.RegisterAsync(dto);
        return CreatedAtAction(nameof(Me), null, result);
    }

    /// <summary>
    /// Autendib kasutaja ja väljastab JWT tokeni.
    /// </summary>
    /// <param name="dto">Sisselogimise andmed.</param>
    /// <response code="200">Edukas sisselogimine.</response>
    /// <response code="401">Vale kasutajanimi või parool.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Tagastab hetkel sisselogitud kasutaja profiili andmed.
    /// </summary>
    /// <response code="200">Kasutaja andmed edukalt tagastatud.</response>
    /// <response code="401">Pole autenditud.</response>
    /// <response code="404">Kasutajat ei leitud.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await authService.GetUserAsync(userId);
        return Ok(user);
    }
}