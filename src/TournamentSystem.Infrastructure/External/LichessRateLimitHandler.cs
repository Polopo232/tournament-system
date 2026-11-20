using System.Net;

namespace TournamentSystem.Infrastructure.External;

/// <summary>
/// Lichessi päringute piiraja: vähemalt 1,1 s vahet päringute vahel ja ootamine 429 vastuse korral.
/// </summary>
public class LichessRateLimitHandler : DelegatingHandler
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime _lastCall = DateTime.MinValue;
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(1100);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            // HttpRequestMessage ei saa uuesti saata, seega uue katse jaoks teeme koopia
            var current = attempt == 1 ? request : Clone(request);

            HttpResponseMessage response;

            await Gate.WaitAsync(cancellationToken);
            try
            {
                var wait = _lastCall + MinInterval - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, cancellationToken);
                }

                response = await base.SendAsync(current, cancellationToken);
                _lastCall = DateTime.UtcNow;
            }
            finally
            {
                Gate.Release();
            }

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= 3)
            {
                return response;
            }

            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
            response.Dispose();
            await Task.Delay(retryAfter, cancellationToken);
        }
    }

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}