using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

public static class TournamentResults
{
    /// <summary>
    /// Matši võitja. Viigi korral (WinnerId puudub) tagastab null.
    /// </summary>
    public static User? GetMatchWinner(Match match)
    {
        if (match.Winner != null) return match.Winner;
        if (match.WinnerId == null) return null;

        return match.WinnerId == match.Player1Id ? match.Player1 : match.Player2;
    }

    /// <summary>
    /// Võit 1 punkt, viik 0,5 punkti. Vabad mängud (bye) ei lähe arvesse.
    /// Võrdsete punktide korral on parem koht see, kellel on väiksem SeedNumber.
    /// </summary>
    public static User? DetermineRoundRobinWinner(Tournament tournament)
    {
        var points = tournament.Registrations.ToDictionary(r => r.PlayerId, _ => 0.0);

        foreach (var m in tournament.Matches
                     .Where(m => m.Status == MatchStatus.Completed && m.Player2Id != null))
        {
            int p2 = m.Player2Id!.Value;

            if (m.WinnerId == m.Player1Id)
            {
                points[m.Player1Id] += 1;
            }
            else if (m.WinnerId == p2)
            {
                points[p2] += 1;
            }
            else
            {
                points[m.Player1Id] += 0.5;
                points[p2] += 0.5;
            }
        }

        return tournament.Registrations
            .OrderByDescending(r => points.GetValueOrDefault(r.PlayerId))
            .ThenBy(r => r.SeedNumber ?? int.MaxValue)
            .FirstOrDefault()?
            .Player;
    }
}