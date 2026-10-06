using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Modules.Assistant.Endpoints;

internal static class AssistantSseWriter
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HttpResponse response,
        string eventName,
        string data,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {eventName}\n", cancellationToken);

        var payload = data.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var line in payload.Split('\n'))
        {
            await response.WriteAsync($"data: {line}\n", cancellationToken);
        }

        await response.WriteAsync("\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
