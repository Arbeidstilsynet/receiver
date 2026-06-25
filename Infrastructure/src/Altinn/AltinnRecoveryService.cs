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

    public Task<AltinnInstance?> GetInstanceMetadata(
        Guid instanceGuid,
        CancellationToken ct = default
    )
    {
        return GetInstanceByGuid(instanceGuid, ct);
    }

    public async Task<IReadOnlyList<DataElement>?> GetDataElementsForInstance(
        Guid instanceGuid,
        CancellationToken ct = default
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var instance = await GetInstanceByGuid(instanceGuid, ct);
        return instance?.Data;
    }

    public async Task<DataElementDownload?> DownloadDataElement(
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var instance = await GetInstanceByGuid(instanceGuid, ct);
        if (instance == null)
            return null;
        var instanceOwnerPartyId = instance.InstanceOwner?.PartyId;
        if (string.IsNullOrWhiteSpace(instanceOwnerPartyId))
            return null;

        var instanceRequest = new InstanceRequest
        {
            InstanceOwnerPartyId = instanceOwnerPartyId,
            InstanceGuid = instanceGuid,
        };

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

    private async Task<AltinnInstance?> GetInstanceByGuid(Guid instanceGuid, CancellationToken ct)
    {
        var registeredApps = await subscriptionRepository.GetAllActiveAltinnSubscriptions();
        foreach (var appId in registeredApps.Select(s => s.AltinnAppId).Distinct())
        {
            string? continuationToken = null;
            do
            {
                ct.ThrowIfCancellationRequested();
                var page = await altinnStorageClient.GetInstances(
                    new InstanceQueryParameters
                    {
                        AppId = appId,
                        ContinuationToken = continuationToken,
                    }
                );

                var matchingInstance = page.Instances.FirstOrDefault(instance =>
                    TryParseInstanceGuid(instance.Id, out var parsedGuid)
                    && parsedGuid == instanceGuid
                );
                if (matchingInstance != null)
                {
                    return matchingInstance;
                }
                continuationToken = GetContinuationToken(page.Next);
            } while (!string.IsNullOrWhiteSpace(continuationToken));
        }
        logger.LogWarning(
            "Could not resolve instance metadata for instance {InstanceGuid}",
            instanceGuid
        );
        return null;
    }

    private static bool TryParseInstanceGuid(string? instanceId, out Guid instanceGuid)
    {
        instanceGuid = default;
        if (string.IsNullOrWhiteSpace(instanceId))
            return false;

        var separatorIndex = instanceId.LastIndexOf('/');
        if (separatorIndex < 0 || separatorIndex == instanceId.Length - 1)
            return false;
        return Guid.TryParse(instanceId[(separatorIndex + 1)..], out instanceGuid);
    }

    private static string? GetContinuationToken(string? next)
    {
        if (string.IsNullOrWhiteSpace(next))
            return null;
        const string tokenParameterName = "continuationToken=";
        var continuationTokenIndex = next.IndexOf(
            tokenParameterName,
            StringComparison.OrdinalIgnoreCase
        );
        if (continuationTokenIndex < 0)
            return null;

        var token = next[(continuationTokenIndex + tokenParameterName.Length)..];
        var ampersandIndex = token.IndexOf('&');
        if (ampersandIndex >= 0)
            token = token[..ampersandIndex];
        return Uri.UnescapeDataString(token);
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
