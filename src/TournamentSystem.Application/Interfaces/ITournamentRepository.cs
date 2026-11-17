using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface ITournamentRepository
{
    Task<List<Tournament>> GetAllAsync();
    Task<Tournament?> GetByIdAsync(int id);
    Task<Tournament?> GetByExternalIdAsync(string externalId);
    Task<bool> IsPlayerRegisteredAsync(int tournamentId, int playerId);
    void Add(Tournament tournament);
    void Remove(Tournament tournament);
    void AddRegistration(Registration registration);
    Task SaveChangesAsync();
}