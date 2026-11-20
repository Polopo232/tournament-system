using Microsoft.EntityFrameworkCore;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Infrastructure.Data;

namespace TournamentSystem.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<List<User>> GetAllAsync()
    {
        return db.Users.OrderBy(u => u.Id).ToListAsync();
    }

    public async Task<(List<User> Items, int Total)> GetPagedAsync(UserQueryParams query)
    {
        var users = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(u =>
                u.DisplayName.Contains(term) ||
                u.Username.Contains(term) ||
                u.Email.Contains(term));
        }

        if (Enum.TryParse<UserRole>(query.Role, true, out var role))
        {
            users = users.Where(u => u.Role == role);
        }

        if (string.Equals(query.Source, "lichess", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(u => u.Username.StartsWith("lichess_"));
        }
        else if (string.Equals(query.Source, "local", StringComparison.OrdinalIgnoreCase))
        {
            users = users.Where(u => !u.Username.StartsWith("lichess_"));
        }

        users = (query.SortBy ?? "name").ToLowerInvariant() switch
        {
            "newest" => users.OrderByDescending(u => u.CreatedAt),
            "role" => users.OrderBy(u => u.Role).ThenBy(u => u.DisplayName),
            _ => users.OrderBy(u => u.DisplayName)
        };

        var total = await users.CountAsync();
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var items = await users
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, total);
    }

    public Task<User?> GetByIdAsync(int id)
    {
        return db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        return db.Users
            .Include(u => u.Ratings)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public Task<User?> GetByDisplayNameAsync(string displayName)
    {
        return db.Users.FirstOrDefaultAsync(u => u.DisplayName == displayName);
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