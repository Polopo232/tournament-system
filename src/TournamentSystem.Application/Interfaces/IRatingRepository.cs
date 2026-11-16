using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface IRatingRepository
{
    Task<List<RatingHistory>> GetByPlayerIdAsync(int playerId);
    Task<List<PlayerRating>> GetPlayerRatingsAsync(int playerId);
    Task<Dictionary<int, PlayerRating>> GetOrCreateForPlayersAsync(IEnumerable<int> playerIds, string game);
    void Add(RatingHistory entry);
    Task SaveChangesAsync();
}