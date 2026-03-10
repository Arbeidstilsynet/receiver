using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IVirusScanService
{
    Task<DocumentScanResult> ScanForVirus(
        UploadResponse persistedDocument,
        CancellationToken cancellationToken
    );
}
