namespace TournamentSystem.Application.DTOs;

/// <summary>Lichessi sünkroonimise kokkuvõte.</summary>
public class LichessSyncResultDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Finished { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }

    /// <summary>Vigade tekstid areeni kaupa, et probleem oleks näha ilma logideta.</summary>
    public List<string> Errors { get; set; } = new();
}