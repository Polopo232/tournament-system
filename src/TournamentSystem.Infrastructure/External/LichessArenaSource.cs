using System.Globalization;
using System.Net;
using System.Text.Json;
using TournamentSystem.Application.Interfaces;

namespace TournamentSystem.Infrastructure.External;

public class LichessArenaSource(HttpClient http) : IExternalArenaSource
{
    public async Task<ExternalArena?> GetArenaAsync(string arenaId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(
            $"/api/tournament/{Uri.EscapeDataString(arenaId)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        var name = GetString(root, "fullName") ?? arenaId;

        return new ExternalArena(
            arenaId,
            name,
            ParseStartsAt(root),
            IsFinished(root));
    }

    public async Task<List<ExternalArenaResult>> GetResultsAsync(string arenaId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(
            $"/api/tournament/{Uri.EscapeDataString(arenaId)}/results", cancellationToken);

        response.EnsureSuccessStatusCode();

        var results = new List<ExternalArenaResult>();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var doc = JsonDocument.Parse(line);
            var item = doc.RootElement;

            // Lichess uses "username" in results and "name" in arena standings, accept both
            var username = GetString(item, "username") ?? GetString(item, "name");
            if (string.IsNullOrWhiteSpace(username)) continue;

            results.Add(new ExternalArenaResult(
                item.GetProperty("rank").GetInt32(),
                username,
                item.GetProperty("score").GetInt32()));
        }

        return results;
    }

    private static bool IsFinished(JsonElement root)
    {
        if (IsTrue(root, "finished") || IsTrue(root, "isFinished"))
        {
            return true;
        }

        // Fallback: the arena has started and no time is left
        bool started = IsTrue(root, "isStarted");
        bool noTimeLeft = !root.TryGetProperty("secondsToFinish", out var seconds)
            || (seconds.ValueKind == JsonValueKind.Number && seconds.GetInt32() <= 0);

        return started && noTimeLeft;
    }

    private static DateTime ParseStartsAt(JsonElement root)
    {
        if (!root.TryGetProperty("startsAt", out var value))
        {
            return DateTime.UtcNow;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => DateTimeOffset.FromUnixTimeMilliseconds(value.GetInt64()).UtcDateTime,
            JsonValueKind.String => DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture).UtcDateTime,
            _ => DateTime.UtcNow
        };
    }

    private static bool IsTrue(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? GetString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}