using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Ports.Clients;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Altinn;

internal class AltinnCompletionAction(IAltinnAppsClient altinnStorageClient)
    : IPostMeldingPersistedAction
{
    public string Name => nameof(AltinnCompletionAction);

    public async Task RunPostActionFor(Melding melding)
    {
        if (melding.Source == MessageSource.Altinn)
        {
            using var activity = Tracer.Source.StartActivity("run AltinnCompletionAction");
            var instanceGuid = melding.GetTag((AltinnMetadata a) => a.InstanceGuid);
            if (string.IsNullOrEmpty(instanceGuid))
                throw new InvalidOperationException(
                    $"No altinn instanceGuid tag found for melding {melding.Id}"
                );
            var instanceOwnerPartyId = melding.GetTag((AltinnMetadata a) => a.InstanceOwnerPartyId);
            if (string.IsNullOrEmpty(instanceOwnerPartyId))
                throw new InvalidOperationException(
                    $"No altinn instanceOwnerPartyId tag found for melding {melding.Id}"
                );

            await altinnStorageClient.CompleteInstance(
                melding.ApplicationId,
                new InstanceRequest
                {
                    InstanceGuid = ParseGuidOrThrow(instanceGuid, melding.Id),
                    InstanceOwnerPartyId = instanceOwnerPartyId,
                }
            );
        }
    }

    private static Guid ParseGuidOrThrow(string guidString, Guid meldingId)
    {
        if (!Guid.TryParse(guidString, out var guid))
            throw new InvalidOperationException(
                $"Invalid altinn instanceGuid format ('{guidString}') for melding {meldingId}"
            );
        return guid;
    }
}
