using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;
using Match = TournamentSystem.Domain.Entities.Match;

namespace TournamentSystem.UnitTests;

public class RoundRobinTests
{
    private readonly Mock<ITournamentRepository> _tournamentRepoMock = new();
    private readonly Mock<IMatchRepository> _matchRepoMock = new();
    private readonly Mock<ITournamentNotifier> _notifierMock = new();
    private readonly Mock<ILogger<BracketService>> _loggerMock = new();
    private readonly BracketService _bracketService;

    public RoundRobinTests()
    {
        _bracketService = new BracketService(
            _tournamentRepoMock.Object,
            _matchRepoMock.Object,
            _notifierMock.Object,
            _loggerMock.Object);
    }

    private static Tournament CreateRoundRobinTournament(int playerCount)
    {
        var tournament = new Tournament
        {
            Id = 1,
            Name = "Round Robin Test",
            Game = "Chess",
            Format = TournamentFormat.RoundRobin,
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

    private static void CompleteCurrentRound(Tournament tournament)
    {
        var pending = tournament.Matches
            .Where(m => m.Round == tournament.CurrentRound && m.Status == MatchStatus.Pending)
            .ToList();

        foreach (var m in pending)
        {
            m.Status = MatchStatus.Completed;
            m.WinnerId = m.Player1Id;
            m.Winner = m.Player1;
        }
    }

    // Меняем рейтинги между раундами: если пары зависят от рейтинга, тест это поймает
    private static void ShuffleRatings(Tournament tournament, int round)
    {
        foreach (var registration in tournament.Registrations)
        {
            registration.Player.Ratings[0].Rating = 1000 + ((registration.PlayerId * 37 + round * 101) % 9) * 100;
        }
    }

    [Theory]
    [InlineData(6)]
    [InlineData(5)]
    public async Task RoundRobin_EveryPairMeetsExactlyOnce(int playerCount)
    {
        var tournament = CreateRoundRobinTournament(playerCount);
        _tournamentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);

        await _bracketService.GenerateBracketAsync(1);

        int totalRounds = playerCount % 2 == 0 ? playerCount - 1 : playerCount;

        for (int round = 2; round <= totalRounds; round++)
        {
            CompleteCurrentRound(tournament);
            ShuffleRatings(tournament, round);
            await _bracketService.GenerateNextRoundAsync(1);
        }

        CompleteCurrentRound(tournament);

        var pairs = tournament.Matches
            .Where(m => m.Player2Id != null)
            .Select(m => (Math.Min(m.Player1Id, m.Player2Id!.Value), Math.Max(m.Player1Id, m.Player2Id!.Value)))
            .ToList();

        int expectedPairs = playerCount * (playerCount - 1) / 2;

        Assert.Equal(expectedPairs, pairs.Count);
        Assert.Equal(expectedPairs, pairs.Distinct().Count());
    }
}