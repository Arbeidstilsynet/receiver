using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Notification;

internal class ValkeyHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public ValkeyHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (_connectionMultiplexer.IsConnected)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Successfully validated valkey connection.")
            );
        }
        else
        {
            return Task.FromResult(HealthCheckResult.Degraded($"Could not reach valkey db."));
        }
    }
}
