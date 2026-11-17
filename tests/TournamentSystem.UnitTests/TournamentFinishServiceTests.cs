using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Security;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Match = TournamentSystem.Domain.Entities.Match;

using Xunit;

namespace TournamentSystem.UnitTests;

public class TournamentFinishServiceTests
{
    private readonly Mock<ITournamentRepository> _tournaments = new();
    private readonly Mock<IRatingService> _ratings = new();
    private readonly Mock<ILogger<TournamentFinishService>> _logger = new();
    private readonly TournamentFinishService _service;

    public TournamentFinishServiceTests()
    {
        _service = new TournamentFinishService(_tournaments.Object, _ratings.Object, _logger.Object);
    }

    private static Match Done(int id, int round, User p1, User p2, int winnerId) => new()
    {
        Id = id,
        TournamentId = 1,
        Round = round,
        Player1Id = p1.Id,
        Player1 = p1,
        Player2Id = p2.Id,
        Player2 = p2,
        WinnerId = winnerId,
        Status = MatchStatus.Completed
    };

    [Fact]
    public async Task Finish_WithPendingMatches_ThrowsAndDoesNotRecalculate()
    {
        var p1 = new User { Id = 1, Username = "a", DisplayName = "Alpha" };
        var p2 = new User { Id = 2, Username = "b", DisplayName = "Beta" };

        var tournament = new Tournament
        {
            Id = 1,
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.InProgress,
            CurrentRound = 1,
            OrganizerId = 5
        };
        tournament.Matches.Add(new Match
        {
            Id = 1,
            TournamentId = 1,
            Round = 1,
            Player1Id = p1.Id,
            Player1 = p1,
            Player2Id = p2.Id,
            Player2 = p2,
            Status = MatchStatus.Pending
        });

        _tournaments.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(tournament);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.FinishAsync(1, new CurrentUser(5, "Organizer")));

        _ratings.Verify(r => r.RecalculateTournamentRatingsAsync(It.IsAny<int>(), It.IsAny<CurrentUser?>()), Times.Never);
    }

    [Fact]
    public async Task Finish_RoundRobin_SetsWinnerWithMostPoints()
    {
        var p1 = new User { Id = 1, Username = "a", DisplayName = "Alpha" };
        var p2 = new User { Id = 2, Username = "b", DisplayName = "Beta" };
        var p3 = new User { Id = 3, Username = "c", DisplayName = "Gamma" };

        var tournament = new Tournament
        {
            Id = 1,
            Format = TournamentFormat.RoundRobin,
            Status = TournamentStatus.InProgress,
            CurrentRound = 3,
            OrganizerId = 5
        };

        tournament.Registrations.Add(new Registration { TournamentId = 1, PlayerId = 1, Player = p1 });
        tournament.Registrations.Add(new Registration { TournamentId = 1, PlayerId = 2, Player = p2 });
        tournament.Registrations.Add(new Registration { TournamentId = 1, PlayerId = 3, Player = p3 });

        // Alpha võidab kaks mängu, Beta ühe
        tournament.Matches.Add(Done(1, 1, p1, p2, winnerId: 1));
        tournament.Matches.Add(Done(2, 2, p1, p3, winnerId: 1));
        tournament.Matches.Add(Done(3, 3, p2, p3, winnerId: 2));

        _tournaments.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(tournament);

        var result = await _service.FinishAsync(1, new CurrentUser(5, "Organizer"));

        Assert.Equal(TournamentStatus.Finished, tournament.Status);
        Assert.Equal(1, tournament.WinnerId);
        Assert.Equal(1, result.WinnerId);
        _ratings.Verify(r => r.RecalculateTournamentRatingsAsync(1, It.IsAny<CurrentUser?>()), Times.Once);
    }

    [Fact]
    public async Task Finish_OtherOrganizer_ThrowsForbidden()
    {
        var tournament = new Tournament
        {
            Id = 1,
            Format = TournamentFormat.RoundRobin,
            Status = TournamentStatus.InProgress,
            OrganizerId = 5
        };

        _tournaments.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(tournament);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.FinishAsync(1, new CurrentUser(9, "Organizer")));
    }
}