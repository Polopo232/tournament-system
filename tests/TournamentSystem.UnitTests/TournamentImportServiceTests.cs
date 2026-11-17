using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;

namespace TournamentSystem.UnitTests;

public class TournamentImportServiceTests
{
    private readonly Mock<ITournamentRepository> _tournaments = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IExternalArenaSource> _arena = new();
    private readonly Mock<ILogger<TournamentImportService>> _logger = new();
    private readonly TournamentImportService _service;

    public TournamentImportServiceTests()
    {
        _service = new TournamentImportService(
            _tournaments.Object,
            _users.Object,
            _arena.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Import_FinishedArena_CreatesFinishedTournamentWithWinner()
    {
        _tournaments.Setup(t => t.GetByExternalIdAsync("lichess:abcd1234"))
            .ReturnsAsync((Tournament?)null);

        _arena.Setup(a => a.GetArenaAsync("abcd1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalArena("abcd1234", "Test Arena", DateTime.UtcNow, true));

        _arena.Setup(a => a.GetResultsAsync("abcd1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalArenaResult>
            {
                new(2, "Hikaru", 8),
                new(1, "Magnus", 10),
                new(3, "Fabi", 6)
            });

        Tournament? saved = null;
        _tournaments.Setup(t => t.Add(It.IsAny<Tournament>()))
            .Callback<Tournament>(t => saved = t);

        await _service.ImportLichessArenaAsync("abcd1234", adminId: 1);

        Assert.NotNull(saved);
        Assert.Equal("lichess:abcd1234", saved!.ExternalId);
        Assert.Equal(TournamentStatus.Finished, saved.Status);
        Assert.Equal(3, saved.Registrations.Count);
        Assert.Equal("Magnus", saved.Winner!.DisplayName);
    }

    [Fact]
    public async Task Import_AlreadyImported_ReturnsExistingWithoutCallingLichess()
    {
        var existing = new Tournament
        {
            Id = 7,
            Name = "Old",
            Game = "Chess",
            Status = TournamentStatus.Finished,
            ExternalId = "lichess:abcd1234"
        };

        _tournaments.Setup(t => t.GetByExternalIdAsync("lichess:abcd1234"))
            .ReturnsAsync(existing);

        var result = await _service.ImportLichessArenaAsync("abcd1234", adminId: 1);

        Assert.Equal(7, result.Id);
        _arena.Verify(a => a.GetArenaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _tournaments.Verify(t => t.Add(It.IsAny<Tournament>()), Times.Never);
    }

    [Fact]
    public async Task Import_UnfinishedArena_ThrowsValidationException()
    {
        _tournaments.Setup(t => t.GetByExternalIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Tournament?)null);

        _arena.Setup(a => a.GetArenaAsync("abcd1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalArena("abcd1234", "Live", DateTime.UtcNow, false));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ImportLichessArenaAsync("abcd1234", adminId: 1));

        _tournaments.Verify(t => t.Add(It.IsAny<Tournament>()), Times.Never);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcd1234; DROP")]
    [InlineData("")]
    public async Task Import_InvalidArenaId_ThrowsBeforeCallingLichess(string arenaId)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ImportLichessArenaAsync(arenaId, adminId: 1));

        _arena.Verify(a => a.GetArenaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sync_OnlyTakesArenasMatchingCategoryAndDuration()
    {
        _arena.Setup(a => a.GetArenaListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalArenaSummary>
            {
                new("abcd1234", "Rapid 10", "rapid", 10, 20, DateTime.UtcNow),
                new("efgh5678", "Bullet 1", "bullet", 1, 20, DateTime.UtcNow),
                new("ijkl9012", "Rapid 30", "rapid", 30, 20, DateTime.UtcNow)
            });

        _tournaments.Setup(t => t.GetByExternalIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Tournament?)null);

        _arena.Setup(a => a.GetResultsAsync("abcd1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalArenaResult> { new(1, "Magnus", 10), new(2, "Hikaru", 8) });

        var result = await _service.SyncLichessAsync(new[] { "rapid" }, 10, actorId: 1);

        Assert.Equal(1, result.Created);
        _tournaments.Verify(t => t.Add(It.Is<Tournament>(x => x.Format == TournamentFormat.Arena
            && x.Status == TournamentStatus.InProgress)), Times.Once);
    }

    [Fact]
    public async Task Sync_FinishedArenaAlreadyStored_IsSkipped()
    {
        _arena.Setup(a => a.GetArenaListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalArenaSummary>
            {
                new("abcd1234", "Rapid 10", "rapid", 10, 30, DateTime.UtcNow)
            });

        _tournaments.Setup(t => t.GetByExternalIdAsync("lichess:abcd1234"))
            .ReturnsAsync(new Tournament { Id = 7, Status = TournamentStatus.Finished, ExternalId = "lichess:abcd1234" });

        var result = await _service.SyncLichessAsync(new[] { "rapid" }, 10, actorId: 1);

        Assert.Equal(1, result.Skipped);
        _arena.Verify(a => a.GetResultsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}