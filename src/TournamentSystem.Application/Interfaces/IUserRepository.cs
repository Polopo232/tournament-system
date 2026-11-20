using TournamentSystem.Application.DTOs;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<(List<User> Items, int Total)> GetPagedAsync(UserQueryParams query);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByDisplayNameAsync(string displayName);
    Task<List<User>> GetByIdsAsync(List<int> ids);
    void Add(User user);
    Task SaveChangesAsync();
}