using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;
using Match = TournamentSystem.Domain.Entities.Match;

namespace TournamentSystem.UnitTests;

public class MatchServiceTests
{
    [Fact]
    public async Task SubmitResultAsync_ValidResult_UpdatesMatchAndCallsSaveChanges()
    {
        var matchRepoMock = new Mock<IMatchRepository>();
        var tournamentRepoMock = new Mock<ITournamentRepository>();
        var notifierMock = new Mock<ITournamentNotifier>();
        var loggerMock = new Mock<ILogger<MatchService>>();

        var player1 = new User { Id = 1, Username = "player1", Rating = 1200 };
        var player2 = new User { Id = 2, Username = "player2", Rating = 1200 };
        var match = new Match
        {
            Id = 10,
            TournamentId = 1,
            Round = 1,
            Player1Id = 1,
            Player1 = player1,
            Player2Id = 2,
            Player2 = player2,
            Status = MatchStatus.Pending
        };

        var tournament = new Tournament
        {
            Id = 1,
            Name = "Test Cup",
            Format = TournamentFormat.SingleElimination,
            Status = TournamentStatus.InProgress,
            CurrentRound = 1,
            Matches = [match]
        };

        matchRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(match);
        tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        var matchService = new MatchService(
            matchRepoMock.Object,
            tournamentRepoMock.Object,
            notifierMock.Object,
            loggerMock.Object);

        var dto = new SubmitMatchResultDto
        {
            Player1Score = 16,
            Player2Score = 10
        };

        var result = await matchService.SubmitResultAsync(10, dto);

        Assert.Equal(16, match.Player1Score);
        Assert.Equal(10, match.Player2Score);
        Assert.Equal(1, match.WinnerId);
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.True(result.IsRoundCompleted);
        matchRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        notifierMock.Verify(n => n.NotifyMatchResultSavedAsync(1, It.IsAny<MatchDto>()), Times.Once);
    }
}