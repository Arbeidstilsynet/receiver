using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IVirusScanService
{
    Task<DocumentScanResult> ScanForVirus(
        UploadResponse persistedDocument,
        CancellationToken cancellationToken
    );
}
