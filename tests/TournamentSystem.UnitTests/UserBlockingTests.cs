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
    public async Task Login_BlockedUser_WithCorrectPassword_ThrowsForbidden()
    {
        var users = new Mock<IUserRepository>();
        var tokens = new Mock<ITokenService>();
        var logger = new Mock<ILogger<AuthService>>();

        var service = new AuthService(users.Object, tokens.Object, logger.Object);

        users.Setup(u => u.GetByUsernameAsync("blocked"))
            .ReturnsAsync(new User
            {
                Id = 9,
                Username = "blocked",
                DisplayName = "Blocked",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
                IsBlocked = true
            });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.LoginAsync(new LoginDto { Username = "blocked", Password = "Pass123!" }));

        tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SetBlocked_OwnAccount_ThrowsValidation()
    {
        var users = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();

        var service = new UserService(users.Object, logger.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SetBlockedAsync(1, true, actorId: 1));

        users.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}