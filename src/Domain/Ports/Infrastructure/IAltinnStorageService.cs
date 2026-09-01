using System.Runtime.InteropServices.ComTypes;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Microsoft.AspNetCore.Http;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IAltinnStorageService
{
    public Task<Instance?> GetInstance(
        Guid instanceId,
        CancellationToken cancellationToken = default
    );
    public Task<IEnumerable<DataElement>?> GetDataElements(
        Guid instanceId,
        CancellationToken cancellationToken = default
    );
    public Task<DataElement?> GetDataElement(
        Guid instanceId,
        Guid dataElementId,
        CancellationToken cancellationToken = default
    );
    public Task<Stream?> GetDataElementContent(
        Guid instanceId,
        Guid dataElementId,
        CancellationToken cancellationToken = default
    );
}
