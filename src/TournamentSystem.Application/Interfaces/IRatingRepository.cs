using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface IRatingRepository
{
    Task<List<RatingHistory>> GetByPlayerIdAsync(int playerId);
    void Add(RatingHistory entry);
    Task SaveChangesAsync();
}