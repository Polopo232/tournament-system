using Microsoft.Extensions.Logging;
using Moq;
using TournamentSystem.Application.DTOs;
using TournamentSystem.Application.Exceptions;
using TournamentSystem.Application.Interfaces;
using TournamentSystem.Application.Services;
using TournamentSystem.Domain.Entities;
using Xunit;

namespace TournamentSystem.UnitTests;

public class UserBlockingTests
{
    [Fact]
    public async Task Login_ActiveBan_ThrowsForbidden()
    {
        var users = new Mock<IUserRepository>();
        var tokens = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();
        var service = new AuthService(users.Object, tokens.Object, logger.Object);

        users.Setup(u => u.GetByUsernameAsync("cheater"))
            .ReturnsAsync(new User
            {
                Id = 9,
                Username = "cheater",
                DisplayName = "Cheater",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                BanReason = BanReason.Cheating,
                BlockedUntil = DateTime.UtcNow.AddYears(5)
            });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.LoginAsync(new LoginDto { Username = "cheater", Password = "Pass123!" }));

        tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public void ExpiredBan_IsNotActive()
    {
        var user = new User
        {
            BanReason = BanReason.Smurfing,
            BlockedUntil = DateTime.UtcNow.AddMinutes(-1)
        };

        Assert.False(user.IsBlocked);
    }

    [Fact]
    public void PermanentBan_IsActive()
    {
        var user = new User
        {
            BanReason = BanReason.BanEvasion,
            BlockedUntil = null
        };

        Assert.True(user.IsBlocked);
    }

    [Fact]
    public void Duration_Permanent_ReturnsNull()
    {
        Assert.Null(BanRules.ResolveBlockedUntil("Permanent", DateTime.UtcNow));
    }

    [Fact]
    public void Duration_Day1_AddsOneDay()
    {
        var now = DateTime.UtcNow;

        var until = BanRules.ResolveBlockedUntil("Day1", now);

        Assert.Equal(now.AddDays(1), until);
    }

    [Fact]
    public async Task SetBlocked_OwnAccount_ThrowsValidation()
    {
        var users = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();
        var service = new UserService(users.Object, logger.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SetBlockedAsync(1,
                new BlockUserDto { IsBlocked = true, Reason = "Cheating", Duration = "Day1" },
                actorId: 1));
    }

    [Fact]
    public async Task SetBlocked_UnknownReason_ThrowsValidation()
    {
        var users = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();
        var service = new UserService(users.Object, logger.Object);

        users.Setup(u => u.GetByIdAsync(2)).ReturnsAsync(new User { Id = 2 });

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SetBlockedAsync(2,
                new BlockUserDto { IsBlocked = true, Reason = "Bogus", Duration = "Day1" },
                actorId: 1));
    }
}