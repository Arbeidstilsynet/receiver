using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Altinn;

internal class AltinnRecoveryService(
    IAltinnAdapter altinnAdapter,
    ISubscriptionsRepository subscriptionRepository,
    ILogger<AltinnRecoveryService> logger
) : IAltinnRecoveryService
{
    public async Task<IEnumerable<AltinnInstanceSummary>?> GetNonCompletedInstancesByAppId(
        string appId
    )
    {
        var appRegistration = await subscriptionRepository.GetActiveAltinnSubscription(appId);
        if (appRegistration == null)
        {
            return [];
        }
        var nonCompletedInstances = await altinnAdapter.GetNonCompletedInstances(
            appId,
            true
        );
        return nonCompletedInstances;
    }

    public Task<
        Dictionary<string, IEnumerable<AltinnInstanceSummary>>
    > GetAllNonCompletedInstancesForRegisteredApps()
    {
        return GetAllNonCompletedInstancesForRegisteredAppsInternal<AltinnInstanceSummary>(
            (appId) =>
                altinnAdapter.GetNonCompletedInstances(
                    appId,
                    true
                )
        );
    }

    public async Task<IEnumerable<AltinnMetadata>?> GetMetadataForNonCompletedInstancesByAppId(
        string appId
    )
    {
        var appRegistration = await subscriptionRepository.GetActiveAltinnSubscription(appId);
        if (appRegistration == null)
        {
            return [];
        }
        var nonCompletedInstances = await altinnAdapter.GetMetadataForNonCompletedInstances(
            appId,
            true
        );
        return nonCompletedInstances;
    }

    public Task<
        Dictionary<string, IEnumerable<AltinnMetadata>>
    > GetMetadataForAllNonCompletedInstancesForRegisteredApps()
    {
        return GetAllNonCompletedInstancesForRegisteredAppsInternal<AltinnMetadata>(
            (appId) =>
                altinnAdapter.GetMetadataForNonCompletedInstances(
                    appId,
                    true
                )
        );
    }

    private async Task<
        Dictionary<string, IEnumerable<T>>
    > GetAllNonCompletedInstancesForRegisteredAppsInternal<T>(
        Func<string, Task<IEnumerable<T>>> getNonCompletedInstances
    )
    {
        using var activity = Tracer.Source.StartActivity();
        Dictionary<string, IEnumerable<T>> allNonCompletedInstances = [];
        var registeredApps = await subscriptionRepository.GetAllActiveAltinnSubscriptions();
        foreach (var registeredApp in registeredApps)
        {
            using var registeredAppActivity = Tracer.Source.StartActivity(
                $"getAllNonCompletedInstancesFor {registeredApp}"
            );
            logger.LogInformation(
                "Running recovery job for registered app '{AppIdentifier}'",
                registeredApp
            );
            var nonCompletedInstances = (
                await getNonCompletedInstances(registeredApp.AltinnAppId)
            ).ToList();
            logger.LogInformation(
                "Found {Count} non completed instances for app '{AppIdentifier}'.",
                nonCompletedInstances.Count,
                registeredApp
            );
            allNonCompletedInstances.Add(registeredApp.AltinnAppId, nonCompletedInstances);
        }
        return allNonCompletedInstances;
    }
}
