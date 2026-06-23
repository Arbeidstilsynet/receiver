using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.Common.Altinn.Ports.Clients;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Altinn;

internal class AltinnRecoveryService(
    IAltinnAdapter altinnAdapter,
    IAltinnStorageClient altinnStorageClient,
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

    public async Task<IReadOnlyList<DataElement>?> GetDataElementsForInstance(
        Guid instanceGuid,
        CancellationToken ct = default
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var instanceOwnerPartyId = await GetInstanceOwnerPartyId(instanceGuid);
        if (instanceOwnerPartyId == null)
        {
            logger.LogWarning(
                "Could not resolve instance owner for instance {InstanceGuid}",
                instanceGuid
            );
            return null;
        }

        var instance = await altinnStorageClient.GetInstance(
            new InstanceRequest
            {
                InstanceOwnerPartyId = instanceOwnerPartyId,
                InstanceGuid = instanceGuid,
            }
        );
        return instance?.Data;
    }

    public async Task<DataElementDownload?> DownloadDataElement(
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var instanceOwnerPartyId = await GetInstanceOwnerPartyId(instanceGuid);
        if (instanceOwnerPartyId == null)
        {
            logger.LogWarning(
                "Could not resolve instance owner for instance {InstanceGuid}",
                instanceGuid
            );
            return null;
        }

        var instanceRequest = new InstanceRequest
        {
            InstanceOwnerPartyId = instanceOwnerPartyId,
            InstanceGuid = instanceGuid,
        };
        var instance = await altinnStorageClient.GetInstance(instanceRequest);
        if (instance == null)
        {
            logger.LogWarning(
                "Instance {InstanceOwnerPartyId}/{InstanceGuid} not found in Altinn",
                instanceOwnerPartyId,
                instanceGuid
            );
            return null;
        }

        var dataElement = instance.Data.FirstOrDefault(d => d.Id == dataElementId.ToString());
        if (dataElement == null)
        {
            logger.LogWarning(
                "Data element {DataElementId} not found on instance {InstanceOwnerPartyId}/{InstanceGuid}",
                dataElementId,
                instanceOwnerPartyId,
                instanceGuid
            );
            return null;
        }

        var stream = await altinnStorageClient.GetInstanceData(
            new InstanceDataRequest { InstanceRequest = instanceRequest, DataId = dataElementId }
        );

        return new DataElementDownload
        {
            Content = stream,
            ContentType = dataElement.ContentType ?? "application/octet-stream",
            Filename = dataElement.Filename,
        };
    }

    private async Task<string?> GetInstanceOwnerPartyId(Guid instanceGuid)
    {
        var metadataByApp = await GetMetadataForAllNonCompletedInstancesForRegisteredApps();
        var metadata = metadataByApp
            .Values.SelectMany(instances => instances)
            .FirstOrDefault(m => m.InstanceGuid == instanceGuid);

        return metadata?.InstanceOwnerPartyId;
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
