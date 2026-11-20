using TournamentSystem.Application.Exceptions;
using TournamentSystem.Domain.Entities;

namespace TournamentSystem.Application.Services;

/// <summary>Blokeeringu põhjused ja kestused.</summary>
public static class BanRules
{
    private static readonly Dictionary<string, TimeSpan?> Durations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Minute1"] = TimeSpan.FromMinutes(1),
        ["Day1"] = TimeSpan.FromDays(1),
        ["Days3"] = TimeSpan.FromDays(3),
        ["Week1"] = TimeSpan.FromDays(7),
        ["Month1"] = TimeSpan.FromDays(30),
        ["Year1"] = TimeSpan.FromDays(365),
        ["Years5"] = TimeSpan.FromDays(365 * 5),
        ["Permanent"] = null
    };

    public static BanReason ParseReason(string? value)
    {
        return Enum.TryParse<BanReason>(value, true, out var reason)
            ? reason
            : throw new ValidationException("Unknown ban reason.");
    }

    /// <summary>Tagastab blokeeringu lõpu. Püsiva blokeeringu korral null.</summary>
    public static DateTime? ResolveBlockedUntil(string? duration, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(duration) || !Durations.TryGetValue(duration, out var span))
        {
            throw new ValidationException("Unknown ban duration.");
        }

        return span == null ? null : nowUtc + span;
    }

    /// <summary>Tekst, mis kuvatakse sisselogimisel blokeeritud kasutajale.</summary>
    public static string Describe(User user)
    {
        var reason = user.BanReason?.ToString() ?? "-";
        var until = user.BlockedUntil.HasValue
            ? user.BlockedUntil.Value.ToString("dd.MM.yyyy HH:mm") + " UTC"
            : "püsivalt";

        return $"Konto on blokeeritud. Põhjus: {reason}. Kehtib kuni: {until}.";
    }
}