using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class RatingRepository(AppDbContext db) : IRatingRepository
{
    public Task<List<RatingHistory>> GetByPlayerAsync(int playerId)
    {
        return db.RatingHistory
            .Where(h => h.PlayerId == playerId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
    }

    public void Add(RatingHistory history)
    {
        db.RatingHistory.Add(history);
    }

    public Task SaveChangesAsync()
    {
        return db.SaveChangesAsync();
    }
}
