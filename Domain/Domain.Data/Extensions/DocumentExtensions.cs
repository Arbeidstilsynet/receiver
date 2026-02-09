namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public static class DocumentExtensions
{
    extension(Document? document)
    {
        public bool IsDocumentSafeToUse => document is { ScanResult: DocumentScanResult.Clean };
    }
}
