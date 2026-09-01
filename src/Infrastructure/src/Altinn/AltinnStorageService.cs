using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Ports.Clients;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Altinn;

internal class AltinnStorageService(IAltinnStorageClient client) : IAltinnStorageService
{
    public async Task<Instance?> GetInstance(
        Guid instanceId,
        CancellationToken cancellationToken = default
    )
    {
        return await client.GetInstance(instanceId, cancellationToken);
    }

    public async Task<IEnumerable<DataElement>?> GetDataElements(
        Guid instanceId,
        CancellationToken cancellationToken = default
    )
    {
        var instance = await GetInstance(instanceId, cancellationToken);

        return instance?.Data;
    }

    public async Task<DataElement?> GetDataElement(
        Guid instanceId,
        Guid dataElementId,
        CancellationToken cancellationToken = default
    )
    {
        var instance = await GetInstance(instanceId, cancellationToken);

        return instance?.Data?.FirstOrDefault(de => de.Id == dataElementId.ToString());
    }

    public async Task<Stream?> GetDataElementContent(
        Guid instanceId,
        Guid dataElementId,
        CancellationToken cancellationToken = default
    )
    {
        return await client.GetInstanceData(
            new InstanceDataRequest()
            {
                InstanceRequest = new InstanceRequest()
                {
                    InstanceGuid = instanceId,
                    InstanceOwnerPartyId = string.Empty,
                },
                DataId = dataElementId,
            },
            cancellationToken
        );
    }
}
