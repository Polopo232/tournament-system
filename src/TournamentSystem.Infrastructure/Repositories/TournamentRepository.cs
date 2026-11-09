using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class TournamentRepository(AppDbContext db) : ITournamentRepository
{
    public Task<List<Tournament>> GetAllAsync()
    {
        return db.Tournaments
            .Include(t => t.Organizer)
            .Include(t => t.Registrations)
            .ToListAsync();
    }

    public Task<Tournament?> GetByIdAsync(int id)
    {
        return db.Tournaments
            .Include(t => t.Organizer)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.Player)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public Task<bool> IsPlayerRegisteredAsync(int tournamentId, int playerId)
    {
        return db.Registrations.AnyAsync(r => r.TournamentId == tournamentId && r.PlayerId == playerId);
    }

    public void Add(Tournament tournament)
    {
        db.Tournaments.Add(tournament);
    }

    public void Remove(Tournament tournament)
    {
        db.Tournaments.Remove(tournament);
    }

    public void AddRegistration(Registration registration)
    {
        db.Registrations.Add(registration);
    }

    public Task SaveChangesAsync()
    {
        return db.SaveChangesAsync();
    }
}
