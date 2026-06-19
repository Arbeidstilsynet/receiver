using Arbeidstilsynet.Common.Altinn.Model.Adapter;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IAltinnRecoveryService
{
    Task<IEnumerable<AltinnMetadata>?> GetMetadataForNonCompletedInstancesByAppId(string appId);
    Task<IEnumerable<AltinnInstanceSummary>?> GetNonCompletedInstancesByAppId(string appId);
    Task<AltinnInstanceSummary?> GetNonCompletedInstanceByAppId(string appId, Guid instanceGuid);
    Task<AltinnDocument?> GetDocumentForNonCompletedInstance(
        string appId,
        Guid instanceGuid,
        Guid documentId
    );
    Task<
        Dictionary<string, IEnumerable<AltinnInstanceSummary>>
    > GetAllNonCompletedInstancesForRegisteredApps();

    Task<
        Dictionary<string, IEnumerable<AltinnMetadata>>
    > GetMetadataForAllNonCompletedInstancesForRegisteredApps();
}
