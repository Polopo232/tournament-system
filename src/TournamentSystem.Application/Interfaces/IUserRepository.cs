using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetByIdsAsync(List<int> ids);
    void Add(User user);
    Task SaveChangesAsync();
}
