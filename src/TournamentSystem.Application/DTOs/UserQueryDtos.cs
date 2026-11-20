namespace TournamentSystem.Application.DTOs;

/// <summary>Kasutajate nimekirja päringu parameetrid.</summary>
public class UserQueryParams
{
    /// <summary>Otsing nicki, kasutajanime või e-posti järgi.</summary>
    public string? Search { get; set; }

    /// <summary>Roll: Admin, Organizer, Player või Service.</summary>
    public string? Role { get; set; }

    /// <summary>Allikas: local (kohalikud) või lichess.</summary>
    public string? Source { get; set; }

    /// <summary>Sortimine: name, newest või role.</summary>
    public string? SortBy { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Kasutajate lehekülg.</summary>
public class UserPageDto
{
    public List<UserDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}