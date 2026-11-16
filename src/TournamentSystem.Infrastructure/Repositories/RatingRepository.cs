using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class RatingRepository(AppDbContext db) : IRatingRepository
{
    public Task<List<RatingHistory>> GetByPlayerIdAsync(int playerId)
    {
        return db.RatingHistory
            .Where(h => h.PlayerId == playerId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
    }

    public Task<List<PlayerRating>> GetPlayerRatingsAsync(int playerId)
    {
        return db.PlayerRatings
            .Where(r => r.UserId == playerId)
            .OrderBy(r => r.Game)
            .ToListAsync();
    }

    public async Task<Dictionary<int, PlayerRating>> GetOrCreateForPlayersAsync(
        IEnumerable<int> playerIds, string game)
    {
        var ids = playerIds.Distinct().ToList();

        var existing = await db.PlayerRatings
            .Where(r => r.Game == game && ids.Contains(r.UserId))
            .ToListAsync();

        var map = existing.ToDictionary(r => r.UserId);

        foreach (var id in ids)
        {
            if (!map.ContainsKey(id))
            {
                var created = new PlayerRating { UserId = id, Game = game, Rating = 1200 };
                db.PlayerRatings.Add(created);
                map[id] = created;
            }
        }

        return map;
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