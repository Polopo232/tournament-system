using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;

namespace TournamentSystem.UnitTests;

public class TournamentServiceTests
{
    private readonly Mock<ITournamentRepository> _tournamentsMock = new();
    private readonly Mock<IUserRepository> _usersMock = new();
    private readonly Mock<ILogger<TournamentService>> _loggerMock = new();
    private readonly TournamentService _service;

    public TournamentServiceTests()
    {
        _service = new TournamentService(
            _tournamentsMock.Object,
            _usersMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterPlayerAsync_WhenTournamentAlreadyStarted_ThrowsValidationException()
    {
        // Arrange
        var tournament = new Tournament
        {
            Id = 1,
            Name = "Started Cup",
            Status = TournamentStatus.InProgress,
            MaxPlayers = 8
        };
        var player = new User { Id = 5, Username = "player5" };

        _tournamentsMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tournament);
        _usersMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(player);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterPlayerAsync(tournamentId: 1, playerId: 5));

        _tournamentsMock.Verify(r => r.AddRegistration(It.IsAny<Registration>()), Times.Never);
    }

    [Fact]
    public async Task RegisterPlayerAsync_WhenTournamentIsFull_ThrowsValidationException()
    {
        // Arrange
        var tournament = new Tournament
        {
            Id = 2,
            Name = "Full Cup",
            Status = TournamentStatus.Registration,
            MaxPlayers = 2,
            Registrations =
            [
                new Registration { PlayerId = 1 },
                new Registration { PlayerId = 2 }
            ]
        };
        var player = new User { Id = 3, Username = "player3" };

        _tournamentsMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(tournament);
        _usersMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(player);
        _tournamentsMock.Setup(r => r.IsPlayerRegisteredAsync(2, 3)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterPlayerAsync(tournamentId: 2, playerId: 3));

        _tournamentsMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}