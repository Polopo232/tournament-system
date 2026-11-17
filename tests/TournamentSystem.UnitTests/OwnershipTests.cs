using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Security;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;

namespace TournamentSystem.UnitTests;

public class OwnershipTests
{
    private readonly Mock<ITournamentRepository> _tournamentsMock = new();
    private readonly Mock<IUserRepository> _usersMock = new();
    private readonly Mock<ILogger<TournamentService>> _loggerMock = new();
    private readonly TournamentService _service;

    public OwnershipTests()
    {
        _service = new TournamentService(
            _tournamentsMock.Object,
            _usersMock.Object,
            _loggerMock.Object);
    }

    private static Tournament CreateTournament(int organizerId) => new()
    {
        Id = 1,
        Name = "Test Cup",
        Game = "Chess",
        Format = TournamentFormat.RoundRobin,
        Status = TournamentStatus.Registration,
        MaxPlayers = 4,
        OrganizerId = organizerId
    };

    [Fact]
    public async Task Delete_OtherOrganizer_ThrowsForbidden()
    {
        _tournamentsMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTournament(organizerId: 10));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.DeleteAsync(1, new CurrentUser(20, "Organizer")));

        _tournamentsMock.Verify(r => r.Remove(It.IsAny<Tournament>()), Times.Never);
    }

    [Fact]
    public async Task Delete_OwnerOrganizer_IsAllowed()
    {
        _tournamentsMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTournament(organizerId: 10));

        await _service.DeleteAsync(1, new CurrentUser(10, "Organizer"));

        _tournamentsMock.Verify(r => r.Remove(It.IsAny<Tournament>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Admin_IsAllowedForAnyTournament()
    {
        _tournamentsMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTournament(organizerId: 10));

        await _service.DeleteAsync(1, new CurrentUser(1, "Admin"));

        _tournamentsMock.Verify(r => r.Remove(It.IsAny<Tournament>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Player_ThrowsForbidden()
    {
        _tournamentsMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTournament(organizerId: 10));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.DeleteAsync(1, new CurrentUser(10, "Player")));
    }
}