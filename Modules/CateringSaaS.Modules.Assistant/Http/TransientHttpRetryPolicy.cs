using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace CateringSaaS.Modules.Assistant.Http;

internal static class TransientHttpRetryPolicy
{
    /// <summary>
    /// Three attempts after the first failure. Delays are 2s, 4s, and 8s.
    /// Covers connection failures, HTTP 408/5xx, and HTTP 429.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
