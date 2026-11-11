using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Lehekülgedeks jaotatud tulemuste üldine mudel.
/// </summary>
public class PagedResultDto<T>
{
    /// <summary>
    /// Konkreetse lehekülje elemendid.
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Elementide koguarv andmebaasis (vastavalt filtritele).
    /// </summary>
    /// <example>25</example>
    public int TotalCount { get; set; }

    /// <summary>
    /// Praegune lehekülje number.
    /// </summary>
    /// <example>1</example>
    public int Page { get; set; }

    /// <summary>
    /// Elementide arv ühel leheküljel.
    /// </summary>
    /// <example>10</example>
    public int PageSize { get; set; }

    /// <summary>
    /// Lehekülgede koguarv.
    /// </summary>
    /// <example>3</example>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Turniiride otsimise, filtreerimise, sorteerimise ja lehekülgede parameetrid.
/// </summary>
public class TournamentQueryParams
{
    /// <summary>
    /// Otsingusõna turniiri nime järgi.
    /// </summary>
    /// <example>Cup</example>
    public string? Search { get; set; }

    /// <summary>
    /// Filtreerimine staatuse järgi (Registration, InProgress, Finished).
    /// </summary>
    /// <example>Registration</example>
    public string? Status { get; set; }

    /// <summary>
    /// Filtreerimine mängu/distsipliini järgi (nt CS2, Chess, TableTennis).
    /// </summary>
    /// <example>CS2</example>
    public string? Discipline { get; set; }

    /// <summary>
    /// Sorteerimise väli (name, name_desc, startDate, maxPlayers).
    /// </summary>
    /// <example>startDate</example>
    public string? SortBy { get; set; }

    /// <summary>
    /// Lehekülje number (vaikimisi 1).
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Elementide arv leheküljel (vaikimisi 10, max 100).
    /// </summary>
    /// <example>10</example>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Uue turniiri loomise mudel.
/// </summary>
public class CreateTournamentDto : IValidatableObject
{
    /// <summary>
    /// Turniiri nimi (ei tohi olla tühi).
    /// </summary>
    /// <example>Tallinn CS2 Cup 2026</example>
    [Required(ErrorMessage = "Turniiri nimi on kohustuslik.")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mäng või distsipliin (nt CS2, Chess).
    /// </summary>
    /// <example>CS2</example>
    [Required(ErrorMessage = "Mäng/distsipliin on kohustuslik.")]
    [StringLength(50)]
    public string Game { get; set; } = string.Empty;

    /// <summary>
    /// Turniiri kirjeldus (valikuline).
    /// </summary>
    /// <example>Ametlik kevadturniir</example>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Turniiri formaat (SingleElimination või RoundRobin).
    /// </summary>
    /// <example>SingleElimination</example>
    [Required]
    [EnumDataType(typeof(TournamentFormat))]
    public string Format { get; set; } = nameof(TournamentFormat.SingleElimination);

    /// <summary>
    /// Maksimaalne mängijate arv (peab olema >= 2 ja paarisarv).
    /// </summary>
    /// <example>16</example>
    [Required]
    [Range(2, 512, ErrorMessage = "Mängijate maksimaalne arv peab olema vähemalt 2.")]
    public int MaxPlayers { get; set; }

    /// <summary>
    /// Turniiri alguskuupäev.
    /// </summary>
    /// <example>2026-12-01T12:00:00Z</example>
    [Required]
    public DateTime StartDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxPlayers % 2 != 0)
        {
            yield return new ValidationResult(
                "Mängijate maksimaalne arv peab olema kahega jagatav (paarisarv).",
                [nameof(MaxPlayers)]);
        }
    }
}

/// <summary>
/// Olemasoleva turniiri uuendamise mudel.
/// </summary>
public class UpdateTournamentDto : IValidatableObject
{
    /// <summary>
    /// Turniiri nimi (ei tohi olla tühi).
    /// </summary>
    /// <example>Tallinn CS2 Cup 2026 - Updated</example>
    [Required(ErrorMessage = "Turniiri nimi on kohustuslik.")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mäng või distsipliin.
    /// </summary>
    /// <example>CS2</example>
    [Required]
    [StringLength(50)]
    public string Game { get; set; } = string.Empty;

    /// <summary>
    /// Turniiri kirjeldus.
    /// </summary>
    /// <example>Uuendatud kirjeldus</example>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Turniiri formaat (SingleElimination või RoundRobin).
    /// </summary>
    /// <example>SingleElimination</example>
    [Required]
    [EnumDataType(typeof(TournamentFormat))]
    public string Format { get; set; } = nameof(TournamentFormat.SingleElimination);

    /// <summary>
    /// Turniiri staatus (Registration, InProgress, Finished).
    /// </summary>
    /// <example>InProgress</example>
    [Required]
    [EnumDataType(typeof(TournamentStatus))]
    public string Status { get; set; } = nameof(TournamentStatus.Registration);

    /// <summary>
    /// Maksimaalne mängijate arv (>= 2 ja paarisarv).
    /// </summary>
    /// <example>16</example>
    [Required]
    [Range(2, 512, ErrorMessage = "Mängijate maksimaalne arv peab olema vähemalt 2.")]
    public int MaxPlayers { get; set; }

    /// <summary>
    /// Turniiri alguskuupäev.
    /// </summary>
    /// <example>2026-12-01T12:00:00Z</example>
    [Required]
    public DateTime StartDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxPlayers % 2 != 0)
        {
            yield return new ValidationResult(
                "Mängijate maksimaalne arv peab olema kahega jagatav (paarisarv).",
                [nameof(MaxPlayers)]);
        }
    }
}

/// <summary>
/// Turniiri detailse info mudel.
/// </summary>
public class TournamentDetailsDto
{
    /// <summary>
    /// Turniiri unikaalne ID.
    /// </summary>
    /// <example>1</example>
    public int Id { get; set; }

    /// <summary>
    /// Turniiri nimi.
    /// </summary>
    /// <example>Tallinn CS2 Cup 2026</example>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mäng / distsipliin.
    /// </summary>
    /// <example>CS2</example>
    public string Game { get; set; } = string.Empty;

    /// <summary>
    /// Turniiri kirjeldus.
    /// </summary>
    /// <example>Ametlik turniir</example>
    public string? Description { get; set; }

    /// <summary>
    /// Turniiri formaat.
    /// </summary>
    /// <example>SingleElimination</example>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Turniiri staatus.
    /// </summary>
    /// <example>Registration</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Maksimaalne mängijate arv.
    /// </summary>
    /// <example>16</example>
    public int MaxPlayers { get; set; }

    /// <summary>
    /// Praegune voor.
    /// </summary>
    /// <example>0</example>
    public int CurrentRound { get; set; }

    /// <summary>
    /// Turniiri algusaeg.
    /// </summary>
    /// <example>2026-12-01T12:00:00Z</example>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Korraldaja kasutaja ID.
    /// </summary>
    /// <example>1</example>
    public int OrganizerId { get; set; }

    /// <summary>
    /// Registreerunud mängijate arv.
    /// </summary>
    /// <example>4</example>
    public int RegisteredPlayersCount { get; set; }

    public static TournamentDetailsDto FromEntity(Tournament t)
    {
        return new TournamentDetailsDto
        {
            Id = t.Id,
            Name = t.Name,
            Game = t.Game,
            Description = t.Description,
            Format = t.Format.ToString(),
            Status = t.Status.ToString(),
            MaxPlayers = t.MaxPlayers,
            CurrentRound = t.CurrentRound,
            StartDate = t.StartDate,
            OrganizerId = t.OrganizerId,
            RegisteredPlayersCount = t.Registrations?.Count ?? 0
        };
    }
}