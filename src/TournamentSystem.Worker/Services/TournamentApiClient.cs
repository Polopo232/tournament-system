using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TournamentSystem.Worker.Services;

public class TournamentApiClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TournamentApiClient> logger)
{
    private string? _jwtToken;

    private record LoginRequest(string Username, string Password);
    private record LoginResponse(string Token);
    public record CompletedRoundDto(int TournamentId, string TournamentName, int CompletedRound);

    public async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_jwtToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _jwtToken);
            return;
        }

        await AuthenticateAsync(cancellationToken);
    }

    public async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        var username = configuration["ApiSettings:Username"] ?? "service";
        var password = configuration["ApiSettings:Password"] ?? "Service123!";

        logger.LogInformation("Worker autendib API-s kasutajana '{Username}'...", username);

        var response = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(username, password),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var authData = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("API ei tagastanud JWT-tokenit.");

        _jwtToken = authData.Token;
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _jwtToken);

        logger.LogInformation("Worker on edukalt autenditud ja JWT-token on salvestatud.");
    }

    public async Task<List<CompletedRoundDto>> GetCompletedRoundsAsync(CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await httpClient.GetAsync("/api/tournaments/rounds-completed", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("JWT-token on aegunud. Uuendan tokenit...");
            await AuthenticateAsync(cancellationToken);
            response = await httpClient.GetAsync("/api/tournaments/rounds-completed", cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<CompletedRoundDto>>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task RecalculateRatingsAsync(int tournamentId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await httpClient.PostAsync(
            $"/api/tournaments/{tournamentId}/ratings/recalculate",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await AuthenticateAsync(cancellationToken);
            response = await httpClient.PostAsync(
                $"/api/tournaments/{tournamentId}/ratings/recalculate",
                null,
                cancellationToken);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task GenerateNextRoundAsync(int tournamentId, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await httpClient.PostAsync(
            $"/api/tournaments/{tournamentId}/rounds/next",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await AuthenticateAsync(cancellationToken);
            response = await httpClient.PostAsync(
                $"/api/tournaments/{tournamentId}/rounds/next",
                null,
                cancellationToken);
        }

        response.EnsureSuccessStatusCode();
    }
}