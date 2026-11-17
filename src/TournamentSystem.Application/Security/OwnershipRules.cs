using TournamentSystem.Application.Exceptions;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Security;

/// <summary>
/// Who performs the action: Retrieved from the JWT in the controller.
/// </summary>
public record CurrentUser(int Id, string Role);

public static class OwnershipRules
{
    /// <summary>
    /// Admin and Service can modify any tournament. Organizer can only modify their own.
    /// If actor = null, the check is skipped (internal calls and tests).
    /// </summary>
    public static void EnsureCanManage(CurrentUser? actor, Tournament tournament)
    {
        if (actor == null) return;

        if (actor.Role is "Admin" or "Service") return;

        if (actor.Role == "Organizer" && tournament.OrganizerId == actor.Id) return;

        throw new ForbiddenException("You can manage only your own tournaments.");
    }
}