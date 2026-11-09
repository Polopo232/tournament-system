using Microsoft.EntityFrameworkCore;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var admin = CreateUser("admin", "Admin123!", UserRole.Admin, 1200);
        var organizer = CreateUser("organizer", "Organizer123!", UserRole.Organizer, 1200);
        var service = CreateUser("service", "Service123!", UserRole.Service, 1200);

        int[] ratings = [1000, 1080, 1150, 1210, 1270, 1340, 1420, 1500];
        var players = new List<User>();
        for (int i = 0; i < ratings.Length; i++)
        {
            players.Add(CreateUser($"player{i + 1}", "Player123!", UserRole.Player, ratings[i]));
        }

        db.Users.AddRange(admin, organizer, service);
        db.Users.AddRange(players);

        var tournament = new Tournament
        {
            Name = "Autumn Chess Cup",
            Game = "Chess",
            Description = "Test tournament with one played round.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.InProgress,
            MaxPlayers = 8,
            CurrentRound = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            Organizer = organizer
        };

        foreach (var player in players)
        {
            tournament.Registrations.Add(new Registration { Player = player });
        }

        tournament.Matches.Add(CreatePlayedMatch(players[0], players[7], 0, 1));
        tournament.Matches.Add(CreatePlayedMatch(players[1], players[6], 1, 0));
        tournament.Matches.Add(CreatePlayedMatch(players[2], players[5], 0, 1));
        tournament.Matches.Add(CreatePlayedMatch(players[3], players[4], 1, 0));

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
    }

    private static User CreateUser(string username, string password, UserRole role, int rating)
    {
        return new User
        {
            Username = username,
            Email = $"{username}@tournament.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Rating = rating
        };
    }

    private static Match CreatePlayedMatch(User player1, User player2, int player1Score, int player2Score)
    {
        return new Match
        {
            Round = 1,
            Player1 = player1,
            Player2 = player2,
            Player1Score = player1Score,
            Player2Score = player2Score,
            Winner = player1Score > player2Score ? player1 : player2,
            Status = MatchStatus.Completed,
            PlayedAt = DateTime.UtcNow.AddHours(-2)
        };
    }
}
