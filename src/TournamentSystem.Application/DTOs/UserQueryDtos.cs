namespace TournamentSystem.Application.DTOs;

/// <summary>Kasutajate nimekirja päringu parameetrid.</summary>
public class UserQueryParams
{
    /// <summary>Otsing nime, logini või e-posti järgi.</summary>
    /// <example>mari</example>
    public string? Search { get; set; }

    /// <summary>Roll: Admin, Organizer, Player või Service.</summary>
    /// <example>Player</example>
    public string? Role { get; set; }

    /// <summary>Allikas: local (kohalikud) või lichess.</summary>
    /// <example>local</example>
    public string? Source { get; set; }

    /// <summary>Sortimine: name, newest või role.</summary>
    /// <example>name</example>
    public string? SortBy { get; set; }

    /// <summary>Lehekülg, alates 1.</summary>
    /// <example>1</example>
    public int Page { get; set; } = 1;

    /// <summary>Ridade arv lehel (1–100).</summary>
    /// <example>15</example>
    public int PageSize { get; set; } = 20;
}

/// <summary>Kasutajate lehekülg.</summary>
public class UserPageDto
{
    /// <summary>Kasutajad sellel lehel.</summary>
    public List<UserDto> Items { get; set; } = new();

    /// <summary>Kõigi filtritele vastavate kasutajate arv.</summary>
    /// <example>42</example>
    public int TotalCount { get; set; }

    /// <summary>Praegune lehekülg.</summary>
    /// <example>1</example>
    public int Page { get; set; }

    /// <summary>Ridade arv lehel.</summary>
    /// <example>15</example>
    public int PageSize { get; set; }

    /// <summary>Lehekülgede koguarv.</summary>
    /// <example>3</example>
    public int TotalPages { get; set; }
}