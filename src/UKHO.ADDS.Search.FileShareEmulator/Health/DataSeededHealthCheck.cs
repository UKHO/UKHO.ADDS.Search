using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.ADDS.Search.FileShareEmulator.Health;

public sealed class DataSeededHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists("/data/.seed.complete")
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Seed sentinel not present."));
    }
}
