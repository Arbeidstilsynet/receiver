using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public record UploadDocumentRequest
{
    public required FileMetadata FileMetadata { get; init; }
    public required Stream InputStream { get; init; }

    public Guid? DocumentId { get; init; }
    public DocumentScanResult ScanResult { get; init; } = DocumentScanResult.Unknown;
    public Dictionary<string, string> Tags { get; init; } = [];
}
