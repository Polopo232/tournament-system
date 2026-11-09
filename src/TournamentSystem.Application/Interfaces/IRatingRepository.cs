using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface IRatingRepository
{
    Task<List<RatingHistory>> GetByPlayerAsync(int playerId);
    void Add(RatingHistory history);
    Task SaveChangesAsync();
}
