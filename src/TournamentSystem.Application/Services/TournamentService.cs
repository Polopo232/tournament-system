using Microsoft.Extensions.Logging;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Domain.Entities;
using static TournamentSystem.Application.DTOs.TournamentDetailsDto;

namespace TournamentSystem.Application.Services;

public class TournamentService(
    ITournamentRepository tournaments,
    IUserRepository users,
    ILogger<TournamentService> logger) : ITournamentService
{
    public async Task<PagedResultDto<TournamentDetailsDto>> GetAllAsync(TournamentQueryParams queryParams)
    {
        var all = await tournaments.GetAllAsync();
        IEnumerable<Tournament> filtered = all;

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            filtered = filtered.Where(t =>
                t.Name.Contains(queryParams.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
        {
            filtered = filtered.Where(t =>
                t.Status.ToString().Equals(queryParams.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Discipline))
        {
            filtered = filtered.Where(t =>
                t.Game.Equals(queryParams.Discipline, StringComparison.OrdinalIgnoreCase));
        }

        filtered = queryParams.SortBy?.ToLowerInvariant() switch
        {
            "name" => filtered.OrderBy(t => t.Name),
            "name_desc" => filtered.OrderByDescending(t => t.Name),
            "maxplayers" => filtered.OrderBy(t => t.MaxPlayers),
            "startdate" => filtered.OrderBy(t => t.StartDate),
            _ => filtered.OrderByDescending(t => t.StartDate)
        };

        var totalCount = filtered.Count();
        var page = Math.Max(1, queryParams.Page);
        var pageSize = Math.Clamp(queryParams.PageSize, 1, 100);

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TournamentDetailsDto.FromEntity)
            .ToList();

        return new PagedResultDto<TournamentDetailsDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TournamentDetailsDto> GetByIdAsync(int id)
    {
        var tournament = await tournaments.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tournament with ID {id} was not found.");

        return TournamentDetailsDto.FromEntity(tournament);
    }

    public async Task<TournamentDetailsDto> CreateAsync(CreateTournamentDto dto, int organizerId)
    {
        if (dto.MaxPlayers < 2 || dto.MaxPlayers % 2 != 0)
        {
            throw new ValidationException("MaxPlayers must be >= 2 and divisible by 2.");
        }

        if (!Enum.TryParse<TournamentFormat>(dto.Format, true, out var parsedFormat))
        {
            throw new ValidationException($"Invalid tournament format: {dto.Format}");
        }

        var tournament = new Tournament
        {
            Name = dto.Name,
            Game = dto.Game,
            Description = dto.Description,
            Format = parsedFormat,
            MaxPlayers = dto.MaxPlayers,
            StartDate = dto.StartDate,
            Status = TournamentStatus.Registration,
            OrganizerId = organizerId
        };

        tournaments.Add(tournament);
        await tournaments.SaveChangesAsync();

        logger.LogInformation("Loodi uus turniir: {Name} (ID: {Id}) kasutaja {UserId} poolt", tournament.Name, tournament.Id, organizerId);
        return TournamentDetailsDto.FromEntity(tournament);
    }

    public async Task<TournamentDetailsDto> UpdateAsync(int id, UpdateTournamentDto dto)
    {
        var tournament = await tournaments.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tournament with ID {id} was not found.");

        if (dto.MaxPlayers < 2 || dto.MaxPlayers % 2 != 0)
        {
            throw new ValidationException("MaxPlayers must be >= 2 and divisible by 2.");
        }

        if (!Enum.TryParse<TournamentFormat>(dto.Format, true, out var parsedFormat))
        {
            throw new ValidationException($"Invalid tournament format: {dto.Format}");
        }

        if (!Enum.TryParse<TournamentStatus>(dto.Status, true, out var parsedStatus))
        {
            throw new ValidationException($"Invalid tournament status: {dto.Status}");
        }

        tournament.Name = dto.Name;
        tournament.Game = dto.Game;
        tournament.Description = dto.Description;
        tournament.Format = parsedFormat;
        tournament.MaxPlayers = dto.MaxPlayers;
        tournament.StartDate = dto.StartDate;
        tournament.Status = parsedStatus;

        await tournaments.SaveChangesAsync();
        logger.LogInformation("Turniir (ID: {Id}) uuendatud", id);

        return TournamentDetailsDto.FromEntity(tournament);
    }

    public async Task DeleteAsync(int id)
    {
        var tournament = await tournaments.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tournament with ID {id} was not found.");

        tournaments.Remove(tournament);
        await tournaments.SaveChangesAsync();

        logger.LogInformation("Turniir (ID: {Id}) kustutatud", id);
    }

    public async Task<RegistrationDto> RegisterPlayerAsync(int tournamentId, int playerId)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId)
            ?? throw new NotFoundException($"Tournament with ID {tournamentId} was not found.");

        var player = await users.GetByIdAsync(playerId)
            ?? throw new NotFoundException($"Player with ID {playerId} was not found.");

        // 1. Tournament don't start yet
        if (tournament.Status != TournamentStatus.Registration)
        {
            throw new ValidationException("Registration is closed because the tournament has already started or finished.");
        }

        // 2. Player don't reg yet
        if (await tournaments.IsPlayerRegisteredAsync(tournamentId, playerId))
        {
            throw new ValidationException("Player is already registered for this tournament.");
        }

        // 3. Control registration count
        if (tournament.Registrations.Count >= tournament.MaxPlayers)
        {
            throw new ValidationException("Tournament is already full.");
        }

        var registration = new Registration
        {
            TournamentId = tournamentId,
            PlayerId = playerId,
            RegisteredAt = DateTime.UtcNow
        };

        tournaments.AddRegistration(registration);
        await tournaments.SaveChangesAsync();

        logger.LogInformation(
            "Mängija {Username} (ID: {PlayerId}) registreerus turniirile {TournamentName} (ID: {TournamentId})",
            player.Username, playerId, tournament.Name, tournamentId);

        return RegistrationDto.FromEntity(registration);
    }
}