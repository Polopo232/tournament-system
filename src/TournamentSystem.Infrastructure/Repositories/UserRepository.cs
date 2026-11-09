using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id)
    {
        return db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        return db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public Task<List<User>> GetByIdsAsync(List<int> ids)
    {
        return db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
    }

    public void Add(User user)
    {
        db.Users.Add(user);
    }

    public Task SaveChangesAsync()
    {
        return db.SaveChangesAsync();
    }
}
