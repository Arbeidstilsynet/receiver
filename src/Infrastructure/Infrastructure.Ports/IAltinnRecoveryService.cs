using Arbeidstilsynet.Common.Altinn.Model.Adapter;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IAltinnRecoveryService
{
    Task<IEnumerable<AltinnMetadata>?> GetMetadataForNonCompletedInstancesByAppId(string appId);
    Task<IEnumerable<AltinnInstanceSummary>?> GetNonCompletedInstancesByAppId(string appId);
    Task<
        Dictionary<string, IEnumerable<AltinnInstanceSummary>>
    > GetAllNonCompletedInstancesForRegisteredApps();

    Task<
        Dictionary<string, IEnumerable<AltinnMetadata>>
    > GetMetadataForAllNonCompletedInstancesForRegisteredApps();
}
