using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Storage;

internal class CloudStorageHealthCheck : IHealthCheck
{
    private readonly StorageClient _storageClient;

    private readonly InfrastructureConfiguration _config;

    public CloudStorageHealthCheck(
        StorageClient client,
        IOptions<InfrastructureConfiguration> options
    )
    {
        _storageClient = client;
        _config = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _storageClient.GetBucketAsync(_config.DocumentStorageConfiguration.BucketName);
            return HealthCheckResult.Healthy("Successfully validated cloud storage connection.");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy(
                $"Could not reach cloud storage. Exception was {e.Message}",
                e
            );
        }
    }
}
