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

    private const int FinishedStatus = 30;

    private enum SyncOutcome { Created, Updated, Finished, Skipped }

    public async Task<LichessSyncResultDto> SyncLichessAsync(IReadOnlyList<string> perfKeys, int minutes, int actorId)
    {
        var arenas = await arenaSource.GetArenaListAsync();

        var wanted = arenas
            .Where(a => perfKeys.Contains(a.PerfKey, StringComparer.OrdinalIgnoreCase)
                        && (a.Status == 20 || a.Status == FinishedStatus)
                        && (minutes == 0 || a.ClockLimitSeconds == minutes * 60))
            .ToList();

        var result = new LichessSyncResultDto();

        foreach (var arena in wanted)
        {
            if (!LichessIdPattern.IsMatch(arena.Id))
            {
                result.Skipped++;
                continue;
            }

            try
            {
                switch (await SyncArenaAsync(arena, actorId))
                {
                    case SyncOutcome.Created: result.Created++; break;
                    case SyncOutcome.Updated: result.Updated++; break;
                    case SyncOutcome.Finished: result.Finished++; break;
                    default: result.Skipped++; break;
                }
            }
            catch (Exception ex)
            {
                // Viga ühes areenas ei peata teisi. Muudatused, mis jäid salvestamata, lähevad järgmise salvestusega kaasa.
                result.Failed++;
                logger.LogError(ex, "Lichessi arena {ArenaId} sünkroonimine ebaõnnestus.", arena.Id);
            }
        }

        logger.LogInformation(
            "Lichessi sünkroon: {Created} uut, {Updated} uuendatud, {Finished} lõppenud, {Skipped} vahele jäetud, {Failed} viga.",
            result.Created, result.Updated, result.Finished, result.Skipped, result.Failed);

        return result;
    }

    private async Task<SyncOutcome> SyncArenaAsync(ExternalArenaSummary arena, int actorId)
    {
        var externalId = $"lichess:{arena.Id}";
        var tournament = await tournaments.GetByExternalIdAsync(externalId);

        // Lõppenud arenat jätame vahele, kui sellel on juba võitja.
        // Kui võitja puudub, uuendame turniiri ja määrame võitja.
        if (tournament?.Status == TournamentStatus.Finished && tournament.WinnerId != null)
        {
            return SyncOutcome.Skipped;
        }

        var isFinished = arena.Status == FinishedStatus;
        var results = await arenaSource.GetResultsAsync(arena.Id);
        var isNew = tournament == null;
        var gameKey = $"Chess ({arena.PerfKey})";

        if (tournament == null)
        {
            tournament = new Tournament
            {
                Name = arena.Name,
                Game = gameKey,
                Description = $"Lichess arena · {arena.PerfKey} · {arena.ClockLimitSeconds / 60} min",
                Format = TournamentFormat.Arena,
                Status = TournamentStatus.InProgress,
                MaxPlayers = Math.Max(1, results.Count),
                CurrentRound = 0,
                StartDate = arena.StartsAt,
                OrganizerId = actorId,
                ExternalId = externalId
            };

            tournaments.Add(tournament);
        }
        else
        {
            tournament.Game = gameKey;
        }

        foreach (var r in results.OrderBy(x => x.Rank))
        {
            var player = await GetOrCreateLichessPlayerAsync(r.Username);

            var registration = tournament.Registrations
                .FirstOrDefault(x => x.Player?.Username == player.Username);

            if (registration == null)
            {
                registration = new Registration
                {
                    Player = player,
                    RegisteredAt = arena.StartsAt
                };
                tournament.Registrations.Add(registration);
            }

            registration.FinalRank = r.Rank;
            registration.Score = r.Score;

            if (r.Rating > 0)
            {
                SetCategoryRating(player, gameKey, r.Rating);
            }
        }

        tournament.MaxPlayers = Math.Max(tournament.MaxPlayers, tournament.Registrations.Count);
        tournament.LastSyncedAt = DateTime.UtcNow;

        if (isFinished)
        {
            tournament.Status = TournamentStatus.Finished;
            tournament.Winner = tournament.Registrations.FirstOrDefault(x => x.FinalRank == 1)?.Player;
            await tournaments.SaveChangesAsync();
            return SyncOutcome.Finished;
        }

        await tournaments.SaveChangesAsync();
        return isNew ? SyncOutcome.Created : SyncOutcome.Updated;
    }

    /// <summary>
    /// Salvestab Lichessi reitingu kategooria järgi. Olemasoleva kirje uuendab, uut loob.
    /// Elo-d see ei muuda.
    /// </summary>
    private static void SetCategoryRating(User player, string game, int rating)
    {
        var entry = player.Ratings.FirstOrDefault(x => x.Game == game);

        if (entry == null)
        {
            player.Ratings.Add(new PlayerRating
            {
                User = player,
                Game = game,
                Rating = rating
            });
        }
        else
        {
            entry.Rating = rating;
        }
    }        if (tournament?.Status == TournamentStatus.Finished)
        {
            return SyncOutcome.Skipped;
        }
}