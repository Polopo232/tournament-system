using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using TournamentSystem.Application.DTOs;
using Xunit;
using Match = TournamentSystem.Domain.Entities.Match;

namespace TournamentSystem.UnitTests;

public class BracketServiceTests
{
    private readonly Mock<ITournamentRepository> _tournamentRepoMock = new();
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<ITournamentNotifier> _notifierMock = new();
    private readonly Mock<ILogger<BracketService>> _loggerMock = new();
    private readonly BracketService _bracketService;

    public BracketServiceTests()
    {
        _bracketService = new BracketService(
            _tournamentRepoMock.Object,
            _matchRepoMock.Object,
            _notifierMock.Object,
            _loggerMock.Object);
    }

    private static Tournament CreateTournamentWithPlayers(int playerCount, TournamentFormat format = TournamentFormat.SingleElimination)
    {
        var tournament = new Tournament
        {
            Id = 1,
            Name = "Test Cup",
            Game = "Chess",
            Format = format,
            Status = TournamentStatus.Registration,
            MaxPlayers = playerCount
        };

        for (int i = 1; i <= playerCount; i++)
        {
            var player = new User
            {
                Id = i,
                Username = $"player{i}",
                DisplayName = $"Nick{i}",
                Ratings = { new PlayerRating { Game = "Chess", Rating = 1000 + i * 100 } }
            };
            tournament.Registrations.Add(new Registration
            {
                Id = i,
                TournamentId = 1,
                PlayerId = i,
                Player = player
            });
        }

        return tournament;
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(8, 4)]
    public async Task GenerateBracketAsync_CreatesCorrectMatchCountFor4And8Players(
        int playerCount,
        int expectedFirstRoundMatches)
    {
        var tournament = CreateTournamentWithPlayers(playerCount);
        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        var result = await _bracketService.GenerateBracketAsync(1);

        Assert.Equal(expectedFirstRoundMatches, result.Count);
        Assert.Equal(expectedFirstRoundMatches, tournament.Matches.Count);
        Assert.All(tournament.Matches, m => Assert.Equal(1, m.Round));
        Assert.Equal(TournamentStatus.InProgress, tournament.Status);

        _matchRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _tournamentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateBracketAsync_SeedsHighestRatedAgainstLowestRated()
    {
        var tournament = CreateTournamentWithPlayers(4);
        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        await _bracketService.GenerateBracketAsync(1);

        // Player 4 has the highest rating (1400), Player 1 has the lowest (1100)
        var topSeedMatch = tournament.Matches.First(m => m.Player1Id == 4 || m.Player2Id == 4);
        int opponentId = topSeedMatch.Player1Id == 4 ? topSeedMatch.Player2Id!.Value : topSeedMatch.Player1Id;

        Assert.Equal(1, opponentId);
    }

    [Fact]
    public async Task AdvanceRoundAsync_AdvancesWinnersToNextRound()
    {
        var tournament = CreateTournamentWithPlayers(4);
        tournament.Status = TournamentStatus.InProgress;
        tournament.CurrentRound = 1;

        var p4 = tournament.Registrations.First(r => r.PlayerId == 4).Player;
        var p3 = tournament.Registrations.First(r => r.PlayerId == 3).Player;
        var p2 = tournament.Registrations.First(r => r.PlayerId == 2).Player;
        var p1 = tournament.Registrations.First(r => r.PlayerId == 1).Player;

        tournament.Matches.Add(new Match
        {
            Id = 10,
            TournamentId = 1,
            Round = 1,
            Player1Id = 4,
            Player1 = p4,
            Player2Id = 1,
            Player2 = p1,
            WinnerId = 4,
            Winner = p4,
            Status = MatchStatus.Completed
        });
        tournament.Matches.Add(new Match
        {
            Id = 11,
            TournamentId = 1,
            Round = 1,
            Player1Id = 3,
            Player1 = p3,
            Player2Id = 2,
            Player2 = p2,
            WinnerId = 3,
            Winner = p3,
            Status = MatchStatus.Completed
        });

        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        var nextRoundMatches = await _bracketService.GenerateNextRoundAsync(1);

        var round2Matches = tournament.Matches.Where(m => m.Round == 2).ToList();
        Assert.Single(round2Matches);
        var finalMatch = round2Matches[0];

        Assert.True(finalMatch.Player1Id is 3 or 4);
        Assert.True(finalMatch.Player2Id is 3 or 4);
        Assert.NotEqual(finalMatch.Player1Id, finalMatch.Player2Id);
        Assert.Equal(2, tournament.CurrentRound);
    }

    [Fact]
    public async Task GenerateNextRoundAsync_EightPlayers_SavesBothMatchesOfRoundTwo()
    {
        var tournament = CreateTournamentWithPlayers(8);
        tournament.Status = TournamentStatus.InProgress;
        tournament.CurrentRound = 1;

        var players = tournament.Registrations.Select(r => r.Player).ToList();

        for (int i = 0; i < 4; i++)
        {
            var p1 = players[i];
            var p2 = players[7 - i];

            tournament.Matches.Add(new Match
            {
                Id = i + 1,
                TournamentId = 1,
                Round = 1,
                Player1Id = p1.Id,
                Player1 = p1,
                Player2Id = p2.Id,
                Player2 = p2,
                WinnerId = p1.Id,
                Winner = p1,
                Status = MatchStatus.Completed
            });
        }

        List<Match>? saved = null;
        _matchRepoMock
            .Setup(r => r.AddRange(It.IsAny<List<Match>>()))
            .Callback<List<Match>>(list => saved = list);

        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        await _bracketService.GenerateNextRoundAsync(1);

        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Count);
        Assert.All(saved, m => Assert.Equal(2, m.Round));
    }

    [Fact]
    public async Task FinalRoundCompleted_SetsTournamentWinner()
    {
        var tournament = CreateTournamentWithPlayers(2);
        tournament.Status = TournamentStatus.InProgress;
        tournament.CurrentRound = 1;

        var p1 = tournament.Registrations[0].Player;
        var p2 = tournament.Registrations[1].Player;

        tournament.Matches.Add(new Match
        {
            Id = 1,
            TournamentId = 1,
            Round = 1,
            Player1Id = p1.Id,
            Player1 = p1,
            Player2Id = p2.Id,
            Player2 = p2,
            WinnerId = p2.Id,
            Winner = p2,
            Status = MatchStatus.Completed
        });

        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        await _bracketService.GenerateNextRoundAsync(1);

        Assert.Equal(TournamentStatus.Finished, tournament.Status);
        Assert.Equal(p2.Id, tournament.WinnerId);
        Assert.Same(p2, tournament.Winner);
    }

    [Fact]
    public async Task GenerateBracketAsync_NotifiesClientsAboutRoundOne()
    {
        var tournament = CreateTournamentWithPlayers(4);
        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        var result = await _bracketService.GenerateBracketAsync(1);

        Assert.Equal(2, result.Count);
        _notifierMock.Verify(n => n.NotifyRoundCreatedAsync(
            1,
            1,
            It.Is<List<MatchDto>>(m => m.Count == 2)),
            Times.Once);
    }
}