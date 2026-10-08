using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace CateringSaaS.Modules.Assistant.Http;

internal static class TransientHttpRetryPolicy
{
    /// <summary>
    /// Retries connection failures, HTTP 408/5xx, and HTTP 429.
    /// For 429 prefers Retry-After, otherwise backs off 8s → 16s → 32s → 48s
    /// (Groq TPM windows are often ~15–20s on free tier).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 4,
                sleepDurationProvider: (attempt, outcome, _) => ResolveDelay(attempt, outcome.Result),
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);

    private static TimeSpan ResolveDelay(int attempt, HttpResponseMessage? response)
    {
        if (response?.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            {
                return delta + TimeSpan.FromMilliseconds(750);
            }

            if (retryAfter?.Date is { } date)
            {
                var until = date - DateTimeOffset.UtcNow;
                if (until > TimeSpan.Zero)
                {
                    return until + TimeSpan.FromMilliseconds(750);
                }
            }

            // attempt 1..4 → 10s, 18s, 28s, 40s
            return TimeSpan.FromSeconds(Math.Min(45, 4 + (attempt * 6) + (attempt * attempt)));
        }

        return TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }
}
