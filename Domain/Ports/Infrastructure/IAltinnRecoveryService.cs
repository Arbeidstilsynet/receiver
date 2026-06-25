using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

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

    Task<AltinnInstance?> GetInstanceMetadata(Guid instanceGuid, CancellationToken ct = default);

    Task<IReadOnlyList<DataElement>?> GetDataElementsForInstance(
        Guid instanceGuid,
        CancellationToken ct = default
    );

    Task<DataElementDownload?> DownloadDataElement(
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    );
}
