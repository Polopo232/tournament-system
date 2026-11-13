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

        // 1. Käimasolev maleturniir (1. voor mängitud -> Worker saab kohe 2. vooru luua)
        var t1 = new Tournament
        {
            Name = "Autumn Chess Cup",
            Game = "Chess",
            Description = "Sügisene kiirmale turniir 8 osalejaga. Esimene voor on mängitud.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.InProgress,
            MaxPlayers = 8,
            CurrentRound = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            Organizer = organizer
        };
        foreach (var p in players) t1.Registrations.Add(new Registration { Player = p });
        t1.Matches.Add(CreatePlayedMatch(players[0], players[7], 0, 1));
        t1.Matches.Add(CreatePlayedMatch(players[1], players[6], 1, 0));
        t1.Matches.Add(CreatePlayedMatch(players[2], players[5], 0, 1));
        t1.Matches.Add(CreatePlayedMatch(players[3], players[4], 1, 0));

        // 2. Avatud CS2 turniir (saab mängijana registreeruda)
        var t2 = new Tournament
        {
            Name = "Tallinn CS2 Open Series #1",
            Game = "CS2",
            Description = "Avatud 1v1 aim-turniir. Registreerimine on avatud kõikidele mängijatele.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.Registration,
            MaxPlayers = 16,
            CurrentRound = 0,
            StartDate = DateTime.UtcNow.AddDays(3),
            Organizer = organizer
        };
        for (int i = 0; i < 6; i++) t2.Registrations.Add(new Registration { Player = players[i] });

        // 3. Avatud Dota 2 turniir
        var t3 = new Tournament
        {
            Name = "Baltic Dota 2 Mid-Only Clash",
            Game = "Dota2",
            Description = "Klassikaline 1v1 mid-lane duelliturniir.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.Registration,
            MaxPlayers = 8,
            CurrentRound = 0,
            StartDate = DateTime.UtcNow.AddDays(5),
            Organizer = organizer
        };
        for (int i = 2; i < 6; i++) t3.Registrations.Add(new Registration { Player = players[i] });

        // 4. Käimasolev lauatennise turniir ootel matšidega skoori sisestamiseks
        var t4 = new Tournament
        {
            Name = "TTHK Table Tennis Masters",
            Game = "TableTennis",
            Description = "Koolisisene lauatennise karikas. Korraldaja saab sisestada jooksvate mängude tulemusi.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.InProgress,
            MaxPlayers = 4,
            CurrentRound = 1,
            StartDate = DateTime.UtcNow.AddHours(-3),
            Organizer = organizer
        };
        t4.Registrations.Add(new Registration { Player = players[4] });
        t4.Registrations.Add(new Registration { Player = players[5] });
        t4.Registrations.Add(new Registration { Player = players[6] });
        t4.Registrations.Add(new Registration { Player = players[7] });
        t4.Matches.Add(CreatePendingMatch(1, players[4], players[7]));
        t4.Matches.Add(CreatePendingMatch(1, players[5], players[6]));

        // 5. Lõppenud CS2 turniir
        var t5 = new Tournament
        {
            Name = "Winter Aim Warmup 2026",
            Game = "CS2",
            Description = "Lõppenud nädalavahetuse soojendusturniir.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.Finished,
            MaxPlayers = 4,
            CurrentRound = 2,
            StartDate = DateTime.UtcNow.AddDays(-10),
            Organizer = organizer
        };
        for (int i = 0; i < 4; i++) t5.Registrations.Add(new Registration { Player = players[i] });
        t5.Matches.Add(CreatePlayedMatch(players[0], players[3], 13, 16, 1));
        t5.Matches.Add(CreatePlayedMatch(players[1], players[2], 16, 11, 1));
        t5.Matches.Add(CreatePlayedMatch(players[3], players[1], 16, 9, 2));

        // 6. Avatud Round Robin maleturniir
        var t6 = new Tournament
        {
            Name = "Grandmaster Blitz League",
            Game = "Chess",
            Description = "Kõik mängivad kõigiga läbi (Round Robin formaat).",
            Format = TournamentFormat.RoundRobin,
            Status = TournamentStatus.Registration,
            MaxPlayers = 6,
            CurrentRound = 0,
            StartDate = DateTime.UtcNow.AddDays(7),
            Organizer = organizer
        };
        t6.Registrations.Add(new Registration { Player = players[6] });
        t6.Registrations.Add(new Registration { Player = players[7] });

        // 7. Avatud CS2 õhtuturniir
        var t7 = new Tournament
        {
            Name = "Friday Night Faceit Cup",
            Game = "CS2",
            Description = "Kiire reedeõhtune turniir parimatele laskuritele.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.Registration,
            MaxPlayers = 8,
            CurrentRound = 0,
            StartDate = DateTime.UtcNow.AddDays(2),
            Organizer = organizer
        };
        for (int i = 0; i < 3; i++) t7.Registrations.Add(new Registration { Player = players[i] });

        // 8. Lõppenud Dota 2 turniir
        var t8 = new Tournament
        {
            Name = "Aegis Sprint Invitational",
            Game = "Dota2",
            Description = "Kutsutud mängijate kiirturniir.",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.Finished,
            MaxPlayers = 4,
            CurrentRound = 2,
            StartDate = DateTime.UtcNow.AddDays(-14),
            Organizer = organizer
        };
        for (int i = 4; i < 8; i++) t8.Registrations.Add(new Registration { Player = players[i] });
        t8.Matches.Add(CreatePlayedMatch(players[4], players[7], 0, 2, 1));
        t8.Matches.Add(CreatePlayedMatch(players[5], players[6], 2, 1, 1));
        t8.Matches.Add(CreatePlayedMatch(players[7], players[5], 2, 0, 2));

        db.Tournaments.AddRange(t1, t2, t3, t4, t5, t6, t7, t8);
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

    private static Match CreatePlayedMatch(User player1, User player2, int player1Score, int player2Score, int round = 1)
    {
        return new Match
        {
            Round = round,
            Player1 = player1,
            Player2 = player2,
            Player1Score = player1Score,
            Player2Score = player2Score,
            Winner = player1Score > player2Score ? player1 : player2,
            Status = MatchStatus.Completed,
            PlayedAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    private static Match CreatePendingMatch(int round, User player1, User player2)
    {
        return new Match
        {
            Round = round,
            Player1 = player1,
            Player2 = player2,
            Status = MatchStatus.Pending
        };
    }
}