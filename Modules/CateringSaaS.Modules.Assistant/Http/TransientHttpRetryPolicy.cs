using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace CateringSaaS.Modules.Assistant.Http;

internal static class TransientHttpRetryPolicy
{
    /// <summary>
    /// Short retries for connection failures, HTTP 408/5xx, and HTTP 429.
    /// Keep total wait well under NetworkTimeout so the UI fails fast with a clear
    /// message instead of spinning on "Analyzing…" for several minutes.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: (attempt, outcome, _) => ResolveDelay(attempt, outcome.Result),
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);

    private static TimeSpan ResolveDelay(int attempt, HttpResponseMessage? response)
    {
        if (response?.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            {
                // Cap so free-tier TPM waits do not freeze the chat for minutes.
                return TimeSpan.FromSeconds(Math.Min(25, delta.TotalSeconds)) + TimeSpan.FromMilliseconds(500);
            }

            if (retryAfter?.Date is { } date)
            {
                var until = date - DateTimeOffset.UtcNow;
                if (until > TimeSpan.Zero)
                {
                    return TimeSpan.FromSeconds(Math.Min(25, until.TotalSeconds)) + TimeSpan.FromMilliseconds(500);
                }
            }

            // attempt 1..2 → ~8s, ~16s
            return TimeSpan.FromSeconds(8 * attempt);
        }

        return TimeSpan.FromSeconds(Math.Pow(2, attempt));
    }
}
