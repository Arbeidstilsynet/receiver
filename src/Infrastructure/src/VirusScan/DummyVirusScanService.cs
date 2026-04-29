using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.VirusScan;

internal class DummyVirusScanService(ILogger logger) : IVirusScanService
{
    public Task<DocumentScanResult> ScanForVirus(
        UploadResponse persistedDocument,
        CancellationToken cancellationToken
    )
    {
        logger.LogWarning(
            "Virus scan was skipped due to enabled flag Infrastructure__SkipVirusScan. This should only be used for testing"
        );
        return Task.FromResult(DocumentScanResult.Clean);
    }
}
