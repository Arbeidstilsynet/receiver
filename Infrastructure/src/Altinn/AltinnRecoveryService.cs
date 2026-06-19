using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Altinn;

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
        var nonCompletedInstances = await altinnAdapter.GetNonCompletedInstances(appId, true);
        return nonCompletedInstances;
    }

    public async Task<AltinnInstanceSummary?> GetNonCompletedInstanceByAppId(
        string appId,
        Guid instanceGuid
    )
    {
        var nonCompletedInstances = await GetNonCompletedInstancesByAppId(appId);
        return nonCompletedInstances?.FirstOrDefault(instance =>
            instance.Metadata.InstanceGuid == instanceGuid
        );
    }

    public async Task<AltinnDocument?> GetDocumentForNonCompletedInstance(
        string appId,
        Guid instanceGuid,
        Guid documentId
    )
    {
        var instance = await GetNonCompletedInstanceByAppId(appId, instanceGuid);
        return instance
            ?.GetDocuments()
            .FirstOrDefault(document => document.FileMetadata.AltinnId == documentId);
    }

    public Task<
        Dictionary<string, IEnumerable<AltinnInstanceSummary>>
    > GetAllNonCompletedInstancesForRegisteredApps()
    {
        return GetAllNonCompletedInstancesForRegisteredAppsInternal<AltinnInstanceSummary>(
            (appId) => altinnAdapter.GetNonCompletedInstances(appId, true)
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
            (appId) => altinnAdapter.GetMetadataForNonCompletedInstances(appId, true)
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

file static class AltinnInstanceSummaryExtensions
{
    public static IEnumerable<AltinnDocument> GetDocuments(this AltinnInstanceSummary instance)
    {
        yield return instance.SkjemaAsPdf;

        if (instance.StructuredData is { } structuredData)
        {
            yield return structuredData;
        }

        foreach (var attachment in instance.Attachments)
        {
            yield return attachment;
        }
    }
}
