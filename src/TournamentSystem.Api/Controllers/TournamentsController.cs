using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using static TournamentSystem.Application.DTOs.TournamentDetailsDto;

namespace TournamentSystem.Api.Controllers;

/// <summary>
/// Turniiride haldamise lõpppunktid (CRUD, otsing, filtreerimine, sorteerimine ja leheküljed).
/// </summary>
[ApiController]
[Route("api/tournaments")]
[Produces("application/json")]
public class TournamentsController(
    ITournamentService tournamentService,
    IBracketService bracketService) : ControllerBase
{
    /// <summary>
    /// Tagastab turniiride nimekirja koos otsingu, filtreerimise, sorteerimise ja lehekülgedeks jaotamisega.
    /// </summary>
    /// <param name="query">Päringuparameetrid (search, status, discipline, sortBy, page, pageSize).</param>
    /// <response code="200">Turniiride lehekülg edukalt tagastatud.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TournamentDetailsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TournamentDetailsDto>>> GetAll([FromQuery] TournamentQueryParams query)
    {
        var result = await tournamentService.GetAllAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Tagastab turniiri detailid ID alusel.
    /// </summary>
    /// <param name="id">Turniiri unikaalne ID.</param>
    /// <response code="200">Turniir leiti ja tagastati.</response>
    /// <response code="404">Turniiri ei leitud.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TournamentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDetailsDto>> GetById(int id)
    {
        var result = await tournamentService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Loob uue turniiri (ainult Organizer ja Admin rollidele).
    /// </summary>
    /// <param name="dto">Turniiri loomise sisendandmed.</param>
    /// <response code="201">Turniir edukalt loodud.</response>
    /// <response code="400">Valideerimise viga (nt tühi nimi või paaritu mängijate arv).</response>
    /// <response code="401">Kasutaja ei ole autenditud.</response>
    /// <response code="403">Kasutajal puudub Organizer või Admin roll.</response>
    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(TournamentDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TournamentDetailsDto>> Create(CreateTournamentDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var created = await tournamentService.CreateAsync(dto, userId);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Uuendab olemasoleva turniiri andmeid.
    /// </summary>
    /// <param name="id">Turniiri ID.</param>
    /// <param name="dto">Turniiri uued andmed.</param>
    /// <response code="200">Turniir edukalt uuendatud.</response>
    /// <response code="400">Valed sisendandmed.</response>
    /// <response code="404">Sellise ID-ga turniiri ei leitud.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(TournamentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDetailsDto>> Update(int id, UpdateTournamentDto dto)
    {
        var updated = await tournamentService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    /// <summary>
    /// Kustutab turniiri süsteemist.
    /// </summary>
    /// <param name="id">Kustutatava turniiri ID.</param>
    /// <response code="204">Turniir edukalt kustutatud.</response>
    /// <response code="404">Sellise ID-ga turniiri ei leitud.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await tournamentService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Registreerib hetkel sisselogitud mängija avatud turniirile.
    /// </summary>
    /// <param name="id">Turniiri unikaalne ID.</param>
    /// <response code="201">Mängija edukalt turniirile registreeritud.</response>
    /// <response code="400">Ärireegli viga (turniir on juba alanud, mängija on juba registreeritud või kohad on täis).</response>
    /// <response code="401">Kasutaja ei ole autenditud.</response>
    /// <response code="404">Turniiri või mängijat ei leitud.</response>
    [HttpPost("{id}/registrations")]
    [Authorize(Roles = "Player,Organizer,Admin")]
    [ProducesResponseType(typeof(RegistrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationDto>> Register(int id)
    {
        var playerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var registration = await tournamentService.RegisterPlayerAsync(id, playerId);

        return StatusCode(StatusCodes.Status201Created, registration);
    }

    /// <summary>
    /// Genereerib turniiritabeli ja 1. vooru mängupaarid Elo-reitingu alusel.
    /// </summary>
    /// <param name="id">Turniiri unikaalne ID.</param>
    /// <response code="200">Turniiritabel ja 1. vooru mängupaarid edukalt loodud.</response>
    /// <response code="400">Turniir on juba alanud või osalejaid on vähem kui 2.</response>
    /// <response code="401">Kasutaja ei ole autenditud.</response>
    /// <response code="403">Puudub Organizer või Admin roll.</response>
    /// <response code="404">Turniiri ei leitud.</response>
    [HttpPost("{id}/bracket")]
    [Authorize(Roles = "Organizer,Admin")]
    [ProducesResponseType(typeof(List<MatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MatchDto>>> GenerateBracket(int id)
    {
        var matches = await bracketService.GenerateBracketAsync(id);
        return Ok(matches);
    }

    /// <summary>
    /// Kogub eelmise vooru võitjad ja moodustab järgmise vooru mängupaarid (või lõpetab turniiri).
    /// </summary>
    /// <param name="id">Turniiri unikaalne ID.</param>
    /// <response code="200">Järgmise vooru mängupaarid loodud (tühi list, kui turniir lõppes).</response>
    /// <response code="400">Kõik praeguse vooru mängud pole veel lõpetatud või turniir pole aktiivne.</response>
    /// <response code="401">Kasutaja ei ole autenditud.</response>
    /// <response code="403">Puudub Service, Organizer või Admin roll.</response>
    /// <response code="404">Turniiri ei leitud.</response>
    [HttpPost("{id}/rounds/next")]
    [Authorize(Roles = "Service,Organizer,Admin")]
    [ProducesResponseType(typeof(List<MatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MatchDto>>> GenerateNextRound(int id)
    {
        var nextMatches = await bracketService.GenerateNextRoundAsync(id);
        return Ok(nextMatches);
    }
}