using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(int id);
    Task<List<Match>> GetByTournamentAsync(int tournamentId);
    Task<List<Match>> GetByRoundAsync(int tournamentId, int round);
    void AddRange(List<Match> matches);
    Task SaveChangesAsync();
}
