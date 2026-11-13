using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class TournamentRepository(AppDbContext db) : ITournamentRepository
{
    public async Task<List<Tournament>> GetAllAsync()
    {
        return await db.Tournaments
            .Include(t => t.Registrations)
            .ToListAsync();
    }

    public async Task<Tournament?> GetByIdAsync(int id)
    {
        return await db.Tournaments
            .Include(t => t.Registrations)
                .ThenInclude(r => r.Player)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Player1)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Player2)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> IsPlayerRegisteredAsync(int tournamentId, int playerId)
    {
        return await db.Registrations
            .AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == playerId);
    }

    public void Add(Tournament tournament) => db.Tournaments.Add(tournament);

    public void Remove(Tournament tournament) => db.Tournaments.Remove(tournament);

    public void AddRegistration(Registration registration) => db.Registrations.Add(registration);

    public async Task SaveChangesAsync() => await db.SaveChangesAsync();
}