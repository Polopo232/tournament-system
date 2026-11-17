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

        var name = root.TryGetProperty("fullName", out var fullName)
            ? fullName.GetString() ?? arenaId
            : arenaId;

        var finished = root.TryGetProperty("finished", out var finishedProp)
            && finishedProp.ValueKind == JsonValueKind.True;

        var startsAtMs = root.TryGetProperty("startsAt", out var startsAt)
            ? startsAt.GetInt64()
            : 0L;

        return new ExternalArena(
            arenaId,
            name,
            DateTimeOffset.FromUnixTimeMilliseconds(startsAtMs).UtcDateTime,
            finished);
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

            results.Add(new ExternalArenaResult(
                item.GetProperty("rank").GetInt32(),
                item.GetProperty("username").GetString()!,
                item.GetProperty("score").GetInt32()));
        }

        return results;
    }
}