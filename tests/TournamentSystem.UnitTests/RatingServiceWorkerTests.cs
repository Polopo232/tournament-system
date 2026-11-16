using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;
using Match = TournamentSystem.Domain.Entities.Match;

namespace TournamentSystem.UnitTests;

public class RatingServiceWorkerTests
{
    private readonly Mock<ITournamentRepository> _tournamentsMock = new();
    private readonly Mock<IUserRepository> _usersMock = new();
    private readonly Mock<IRatingRepository> _ratingsMock = new();
    private readonly Mock<IMatchRepository> _matchesMock = new();
    private readonly Mock<ILogger<RatingService>> _loggerMock = new();
    private readonly RatingService _service;

    public RatingServiceWorkerTests()
    {
        _service = new RatingService(
            _tournamentsMock.Object,
            _usersMock.Object,
            _ratingsMock.Object,
            _matchesMock.Object,
            new EloCalculatorService(),
            _loggerMock.Object);
    }

    private static Tournament CreateInProgressTournament(int currentRound)
    {
        return new Tournament
        {
            Id = 1,
            Name = "Test Cup",
            Game = "Chess",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.InProgress,
            CurrentRound = currentRound
        };
    }

    private static Match CompletedMatch(int id, int round, bool ratingsUpdated)
    {
        var p1 = new User { Id = 1, Username = "a", DisplayName = "Alpha" };
        var p2 = new User { Id = 2, Username = "b", DisplayName = "Beta" };

        return new Match
        {
            Id = id,
            TournamentId = 1,
            Round = round,
            Player1Id = 1,
            Player1 = p1,
            Player2Id = 2,
            Player2 = p2,
            WinnerId = 1,
            Status = MatchStatus.Completed,
            RatingsUpdated = ratingsUpdated
        };
    }

    private static Match PendingMatch(int id, int round)
    {
        return new Match
        {
            Id = id,
            TournamentId = 1,
            Round = round,
            Player1Id = 1,
            Player2Id = 2,
            Status = MatchStatus.Pending
        };
    }

    private void SetupTournament(Tournament tournament)
    {
        _tournamentsMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Tournament> { tournament });
        _tournamentsMock.Setup(r => r.GetByIdAsync(tournament.Id))
            .ReturnsAsync(tournament);
    }

    [Fact]
    public async Task RoundFinished_RatingsNotCalculated_IsReturned()
    {
        var tournament = CreateInProgressTournament(1);
        tournament.Matches.Add(CompletedMatch(1, 1, ratingsUpdated: false));
        SetupTournament(tournament);

        var result = await _service.GetCompletedRoundsTournamentsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task RatingsCalculatedButNextRoundMissing_IsStillReturned()
    {
        // Main bug case: Elo has already been recalculated, but the next round hasn't been created
        var tournament = CreateInProgressTournament(1);
        tournament.Matches.Add(CompletedMatch(1, 1, ratingsUpdated: true));
        SetupTournament(tournament);

        var result = await _service.GetCompletedRoundsTournamentsAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].CompletedRound);
    }

    [Fact]
    public async Task NextRoundAlreadyExists_IsNotReturned()
    {
        var tournament = CreateInProgressTournament(1);
        tournament.Matches.Add(CompletedMatch(1, 1, ratingsUpdated: true));
        tournament.Matches.Add(PendingMatch(2, 2));
        SetupTournament(tournament);

        var result = await _service.GetCompletedRoundsTournamentsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RoundNotFinished_IsNotReturned()
    {
        var tournament = CreateInProgressTournament(1);
        tournament.Matches.Add(CompletedMatch(1, 1, ratingsUpdated: false));
        tournament.Matches.Add(PendingMatch(2, 1));
        SetupTournament(tournament);

        var result = await _service.GetCompletedRoundsTournamentsAsync();

        Assert.Empty(result);
    }
}