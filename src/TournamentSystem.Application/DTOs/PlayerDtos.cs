namespace TournamentSystem.Application.DTOs;

/// <summary>Mängija avalik profiil.</summary>
public class PlayerProfileDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public List<PlayerRatingDto> Ratings { get; set; } = new();
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