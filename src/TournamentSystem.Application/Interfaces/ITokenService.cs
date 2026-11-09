using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
