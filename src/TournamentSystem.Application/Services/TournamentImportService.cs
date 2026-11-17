using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public class TournamentImportService(
    ITournamentRepository tournaments,
    IUserRepository users,
    IExternalArenaSource arenaSource,
    ILogger<TournamentImportService> logger) : ITournamentImportService
{
    private static readonly Regex LichessIdPattern = new("^[A-Za-z0-9]{8}$", RegexOptions.Compiled);

    public async Task<TournamentDetailsDto> ImportLichessArenaAsync(string arenaId, int adminId)
    {
        if (!LichessIdPattern.IsMatch(arenaId))
        {
            throw new ValidationException("Lichess arena ID must be 8 letters or digits.");
        }

        var externalId = $"lichess:{arenaId}";

        var existing = await tournaments.GetByExternalIdAsync(externalId);
        if (existing != null)
        {
            logger.LogInformation("Turniir {ExternalId} on juba imporditud (ID: {Id}).", externalId, existing.Id);
            return TournamentDetailsDto.FromEntity(existing);
        }

        var arena = await arenaSource.GetArenaAsync(arenaId)
            ?? throw new NotFoundException($"Lichess arena {arenaId} was not found.");

        if (!arena.Finished)
        {
            throw new ValidationException("The arena has not finished yet. Only finished arenas can be imported.");
        }

        var results = await arenaSource.GetResultsAsync(arenaId);
        if (results.Count == 0)
        {
            throw new ValidationException("The arena has no results to import.");
        }

        var tournament = new Tournament
        {
            Name = arena.Name,
            Game = "Chess",
            Description = $"Imporditud Lichessi turniir ({arenaId}).",
            Format = TournamentFormat.RoundRobin,
            Status = TournamentStatus.Finished,
            MaxPlayers = results.Count,
            CurrentRound = 0,
            StartDate = arena.StartsAt,
            OrganizerId = adminId,
            ExternalId = externalId
        };

        foreach (var result in results.OrderBy(r => r.Rank))
        {
            var player = await GetOrCreateLichessPlayerAsync(result.Username);

            tournament.Registrations.Add(new Registration
            {
                Player = player,
                RegisteredAt = arena.StartsAt,
                FinalRank = result.Rank,
                Score = result.Score
            });
        }

        tournament.Winner = tournament.Registrations
            .FirstOrDefault(r => r.FinalRank == 1)?.Player;

        tournaments.Add(tournament);
        await tournaments.SaveChangesAsync();

        logger.LogInformation("Imporditud Lichessi turniir {ExternalId} ({Players} mängijat).",
            externalId, results.Count);

        return TournamentDetailsDto.FromEntity(tournament);
    }

    private async Task<User> GetOrCreateLichessPlayerAsync(string lichessName)
    {
        var login = $"lichess_{lichessName}".ToLowerInvariant();
        if (login.Length > 50) login = login[..50];

        var existing = await users.GetByUsernameAsync(login);
        if (existing != null) return existing;

        var user = new User
        {
            Username = login,
            DisplayName = await MakeUniqueDisplayNameAsync(lichessName),
            Email = $"{login}@lichess.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            Role = UserRole.Player
        };

        users.Add(user);
        return user;
    }

    private async Task<string> MakeUniqueDisplayNameAsync(string baseName)
    {
        var trimmed = baseName.Length > 20 ? baseName[..20] : baseName;
        if (trimmed.Length < 3) trimmed = trimmed.PadRight(3, '_');

        var candidate = trimmed;
        int counter = 2;

        while (await users.GetByDisplayNameAsync(candidate) != null)
        {
            var suffix = $"_{counter++}";
            candidate = trimmed[..Math.Min(trimmed.Length, 20 - suffix.Length)] + suffix;
        }

        return candidate;
    }
}