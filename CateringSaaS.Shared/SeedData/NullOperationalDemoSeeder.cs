using CateringSaaS.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace CateringSaaS.Shared.SeedData;

/// <summary>
/// No-op demo seeder used until the host registers a real implementation.
/// </summary>
public sealed class NullOperationalDemoSeeder : IOperationalDemoSeeder
{
    private readonly ILogger<NullOperationalDemoSeeder> _logger;

    public NullOperationalDemoSeeder(ILogger<NullOperationalDemoSeeder> logger)
    {
        _logger = logger;
    }

    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Operational demo seeder is not registered; skipping.");
        return Task.CompletedTask;
    }
}
