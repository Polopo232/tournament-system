using System.ComponentModel.DataAnnotations;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.DTOs;

/// <summary>
/// Lehekülgedeks jaotatud tulemuste üldine mudel.
/// </summary>
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Turniiride otsimise, filtreerimise, sorteerimise ja lehekülgede parameetrid.
/// </summary>
public class TournamentQueryParams
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Discipline { get; set; }
    public string? SortBy { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Uue turniiri loomise mudel.
/// </summary>
public class CreateTournamentDto : IValidatableObject
{
    [Required(ErrorMessage = "Turniiri nimi on kohustuslik.")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mäng/distsipliin on kohustuslik.")]
    [StringLength(50)]
    public string Game { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [EnumDataType(typeof(TournamentFormat))]
    public string Format { get; set; } = nameof(TournamentFormat.SingleElimination);

    [Required]
    [Range(2, 512, ErrorMessage = "Mängijate maksimaalne arv peab olema vähemalt 2.")]
    public int MaxPlayers { get; set; }

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
    [Required(ErrorMessage = "Turniiri nimi on kohustuslik.")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Game { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [EnumDataType(typeof(TournamentFormat))]
    public string Format { get; set; } = nameof(TournamentFormat.SingleElimination);

    [Required]
    [EnumDataType(typeof(TournamentStatus))]
    public string Status { get; set; } = nameof(TournamentStatus.Registration);

    [Required]
    [Range(2, 512, ErrorMessage = "Mängijate maksimaalne arv peab olema vähemalt 2.")]
    public int MaxPlayers { get; set; }

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
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentRound { get; set; }
    public DateTime StartDate { get; set; }
    public int OrganizerId { get; set; }
    public int RegisteredPlayersCount { get; set; }

    public List<RegistrationDto> Registrations { get; set; } = [];
    public List<TournamentMatchDto> Matches { get; set; } = [];

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
            RegisteredPlayersCount = t.Registrations?.Count ?? 0,
            Registrations = t.Registrations?
                .Select(RegistrationDto.FromEntity)
                .ToList() ?? [],
            Matches = t.Matches?
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Id)
                .Select(TournamentMatchDto.FromEntity)
                .ToList() ?? []
        };
    }

    public class RegistrationDto
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int PlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime RegisteredAt { get; set; }

        public static RegistrationDto FromEntity(Registration r)
        {
            return new RegistrationDto
            {
                Id = r.Id,
                TournamentId = r.TournamentId,
                PlayerId = r.PlayerId,
                Username = r.Player?.Username ?? $"Mängija #{r.PlayerId}",
                Rating = r.Player?.Rating ?? 1200,
                RegisteredAt = r.RegisteredAt
            };
        }
    }

    public class TournamentMatchDto
    {
        public int Id { get; set; }
        public int Round { get; set; }
        public int Player1Id { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public int? Player2Id { get; set; }
        public string? Player2Name { get; set; }
        public int? Player1Score { get; set; }
        public int? Player2Score { get; set; }
        public string Status { get; set; } = string.Empty;

        public static TournamentMatchDto FromEntity(Match m)
        {
            return new TournamentMatchDto
            {
                Id = m.Id,
                Round = m.Round,
                Player1Id = m.Player1Id,
                Player1Name = m.Player1?.Username ?? $"Mängija #{m.Player1Id}",
                Player2Id = m.Player2Id,
                Player2Name = m.Player2?.Username,
                Player1Score = m.Player1Score,
                Player2Score = m.Player2Score,
                Status = m.Status.ToString()
            };
        }
    }
}