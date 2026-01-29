using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IVirusScanService
{
    Task ScanForVirus(UploadResponse persistedDocument, CancellationToken cancellationToken);
}
