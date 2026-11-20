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
    /// <summary>Otsing turniiri nime järgi.</summary>
    /// <example>cup</example>
    public string? Search { get; set; }

    /// <summary>Staatus: Registration, InProgress või Finished.</summary>
    /// <example>Registration</example>
    public string? Status { get; set; }

    /// <summary>Mäng: Chess, CS2, Dota2 või TableTennis. Chess leiab ka Chess (rapid) arenad.</summary>
    /// <example>CS2</example>
    public string? Discipline { get; set; }

    /// <summary>Formaat: SingleElimination, RoundRobin või Arena.</summary>
    /// <example>SingleElimination</example>
    public string? Format { get; set; }

    /// <summary>Sortimine: startdate, participants, name, maxplayers. Tühi väärtus annab uuemad enne.</summary>
    /// <example>participants</example>
    public string? SortBy { get; set; }

    /// <summary>Lehekülg, alates 1.</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Ridade arv lehel (1–100).</summary>
    /// <example>10</example>
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Uue turniiri loomise mudel.
/// </summary>
public class CreateTournamentDto : IValidatableObject
{
    /// <summary>Turniiri nimi (3–100 märki).</summary>
    /// <example>Tallinn CS2 Weekend Cup</example>
    [Required(ErrorMessage = "Turniiri nimi on kohustuslik.")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Mäng või distsipliin: Chess, CS2, Dota2, TableTennis.</summary>
    /// <example>CS2</example>
    [Required(ErrorMessage = "Mäng/distsipliin on kohustuslik.")]
    [StringLength(50)]
    public string Game { get; set; } = string.Empty;

    /// <summary>Lühikirjeldus (kuni 500 märki).</summary>
    /// <example>Avatud 1v1 aim-turniir kõigile mängijatele.</example>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Turniiri formaat: SingleElimination või RoundRobin.</summary>
    /// <example>SingleElimination</example>
    [Required]
    [EnumDataType(typeof(TournamentFormat))]
    public string Format { get; set; } = nameof(TournamentFormat.SingleElimination);

    /// <summary>Osalejate maksimaalne arv (paarisarv, 2–512).</summary>
    /// <example>8</example>
    [Required]
    [Range(2, 512, ErrorMessage = "Mängijate maksimaalne arv peab olema vähemalt 2.")]
    public int MaxPlayers { get; set; }

    /// <summary>Turniiri algusaeg (ISO 8601, UTC).</summary>
    /// <example>2026-12-01T18:00:00Z</example>
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
    /// <summary>Turniiri nimi (3–100 märki).</summary>
    /// <example>Tallinn CS2 Weekend Cup</example>
    [Required(ErrorMessage = "Turniiri nimi on kohustuslik.")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Mäng või distsipliin.</summary>
    /// <example>CS2</example>
    [Required]
    [StringLength(50)]
    public string Game { get; set; } = string.Empty;

    /// <summary>Lühikirjeldus (kuni 500 märki).</summary>
    /// <example>Kiire reedeõhtune turniir.</example>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Turniiri formaat: SingleElimination või RoundRobin.</summary>
    /// <example>SingleElimination</example>
    [Required]
    [EnumDataType(typeof(TournamentFormat))]
    public string Format { get; set; } = nameof(TournamentFormat.SingleElimination);

    /// <summary>Turniiri staatus: Registration, InProgress või Finished.</summary>
    /// <example>Registration</example>
    [Required]
    [EnumDataType(typeof(TournamentStatus))]
    public string Status { get; set; } = nameof(TournamentStatus.Registration);

    /// <summary>Osalejate maksimaalne arv (paarisarv, 2–512).</summary>
    /// <example>8</example>
    [Required]
    [Range(2, 512, ErrorMessage = "Mängijate maksimaalne arv peab olema vähemalt 2.")]
    public int MaxPlayers { get; set; }

    /// <summary>Turniiri algusaeg (ISO 8601, UTC).</summary>
    /// <example>2026-12-01T18:00:00Z</example>
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

    /// <summary>
    /// Võitja ID (null, kui turniir pole veel lõppenud).
    /// </summary>
    public int? WinnerId { get; set; }

    /// <summary>
    /// Võitja nick.
    /// </summary>
    public string? WinnerName { get; set; }
    public int MaxPlayers { get; set; }
    public int CurrentRound { get; set; }
    public DateTime StartDate { get; set; }
    public int OrganizerId { get; set; }
    public int RegisteredPlayersCount { get; set; }

    public List<RegistrationDto> Registrations { get; set; } = [];
    public List<TournamentMatchDto> Matches { get; set; } = [];
    public List<LiveDuelDto> LiveDuels { get; set; } = new();

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
            WinnerId = t.WinnerId,
            WinnerName = t.Winner?.DisplayName,
            MaxPlayers = t.MaxPlayers,
            CurrentRound = t.CurrentRound,
            StartDate = t.StartDate,
            OrganizerId = t.OrganizerId,
            RegisteredPlayersCount = t.Registrations?.Count ?? 0,
            Registrations = t.Registrations?
                .Select(r => RegistrationDto.FromEntity(r, t.Game))
                .ToList() ?? [],
            Matches = t.Matches?
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Id)
                .Select(TournamentMatchDto.FromEntity)
                .ToList() ?? [],
            LiveDuels = t.Duels.Select(d => new LiveDuelDto
            {
                GameId = d.GameId,
                WhiteName = d.WhiteName,
                WhiteRating = d.WhiteRating,
                BlackName = d.BlackName,
                BlackRating = d.BlackRating
            }).ToList(),
        };
    }

    public class RegistrationDto
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int PlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int? FinalRank { get; set; }
        public int? Score { get; set; }
        public DateTime RegisteredAt { get; set; }

        public static RegistrationDto FromEntity(Registration r, string game)
        {
            var rating = r.Player?.Ratings.FirstOrDefault(x => x.Game == game)?.Rating ?? 1200;

            return new RegistrationDto
            {
                Id = r.Id,
                TournamentId = r.TournamentId,
                PlayerId = r.PlayerId,
                Username = r.Player?.Username ?? $"Mängija #{r.PlayerId}",
                DisplayName = r.Player?.DisplayName ?? $"Mängija #{r.PlayerId}",
                Rating = rating,
                FinalRank = r.FinalRank,
                Score = r.Score,
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
                Player1Name = m.Player1?.DisplayName ?? $"Mängija #{m.Player1Id}",
                Player2Id = m.Player2Id,
                Player2Name = m.Player2?.DisplayName,
                Player1Score = m.Player1Score,
                Player2Score = m.Player2Score,
                Status = m.Status.ToString()
            };
        }
    }

    public class LiveDuelDto
    {
        public string GameId { get; set; } = string.Empty;
        public string WhiteName { get; set; } = string.Empty;
        public int WhiteRating { get; set; }
        public string BlackName { get; set; } = string.Empty;
        public int BlackRating { get; set; }
        public string Url => $"https://lichess.org/{GameId}";
    }
}