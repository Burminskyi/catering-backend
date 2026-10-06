using Microsoft.AspNetCore.Http;

namespace CateringSaaS.Shared.MultiTenancy;

/// <summary>
/// Client wall clock from the <c>X-TimeZone</c> header (IANA). Falls back to Europe/Kyiv.
/// </summary>
public interface IClientTimeContext
{
    string TimeZoneId { get; }

    TimeZoneInfo TimeZone { get; }

    DateTimeOffset LocalNow { get; }

    DateOnly LocalToday { get; }

    /// <summary>Converts a client-local date and time of day to UTC.</summary>
    DateTime ToUtc(DateOnly date, TimeOnly time);
}

public sealed class HttpClientTimeContext : IClientTimeContext
{
    public const string HeaderName = "X-TimeZone";
    public const string FallbackTimeZoneId = "Europe/Kyiv";

    private readonly TimeZoneInfo _timeZone;
    private readonly string _timeZoneId;

    public HttpClientTimeContext(IHttpContextAccessor httpContextAccessor)
    {
        var requested = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
        (_timeZoneId, _timeZone) = Resolve(requested);
    }

    public string TimeZoneId => _timeZoneId;

    public TimeZoneInfo TimeZone => _timeZone;

    public DateTimeOffset LocalNow => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);

    public DateOnly LocalToday => DateOnly.FromDateTime(LocalNow.DateTime);

    public DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
    }

    private static (string Id, TimeZoneInfo Zone) Resolve(string? requested)
    {
        if (TryFind(requested, out var id, out var zone))
        {
            return (id, zone);
        }

        if (TryFind(FallbackTimeZoneId, out id, out zone))
        {
            return (id, zone);
        }

        return ("UTC", TimeZoneInfo.Utc);
    }

    private static bool TryFind(string? id, out string resolvedId, out TimeZoneInfo zone)
    {
        resolvedId = string.Empty;
        zone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        var trimmed = id.Trim();
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
            resolvedId = trimmed;
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
