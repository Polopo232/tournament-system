namespace TournamentSystem.Application.DTOs;

/// <summary>Mängija avalik profiil.</summary>
public class PlayerProfileDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<PlayerRatingDto> Ratings { get; set; } = new();
    public List<PlayerWinDto> Wins { get; set; } = new();

    /// <summary>Blokeering, kui konto on praegu blokeeritud. Muul juhul tühi.</summary>
    public BanInfoDto? Ban { get; set; }
}

/// <summary>Aktiivne blokeering, mis kuvatakse profiilil.</summary>
public class BanInfoDto
{
    /// <example>Cheating</example>
    public string Reason { get; set; } = string.Empty;

    public DateTime? BlockedUntil { get; set; }

    public bool IsPermanent => BlockedUntil == null;
}

/// <summary>Edetabeli rida.</summary>
public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int PlayerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public int Rating { get; set; }
}

/// <summary>Edetabeli lehekülg.</summary>
public class LeaderboardPageDto
{
    public List<LeaderboardEntryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>Mängija võit turniiril.</summary>
public class PlayerWinDto
{
    public int TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}