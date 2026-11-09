using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class MatchRepository(AppDbContext db) : IMatchRepository
{
    public Task<Match?> GetByIdAsync(int id)
    {
        return db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public Task<List<Match>> GetByTournamentAsync(int tournamentId)
    {
        return db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Where(m => m.TournamentId == tournamentId)
            .OrderBy(m => m.Round)
            .ThenBy(m => m.Id)
            .ToListAsync();
    }

    public Task<List<Match>> GetByRoundAsync(int tournamentId, int round)
    {
        return db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Where(m => m.TournamentId == tournamentId && m.Round == round)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    public void AddRange(List<Match> matches)
    {
        db.Matches.AddRange(matches);
    }

    public Task SaveChangesAsync()
    {
        return db.SaveChangesAsync();
    }
}
