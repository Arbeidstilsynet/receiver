using Arbeidstilsynet.Common.Altinn.Extensions;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using AltinnFileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;
using DocumentFileMetadata = Arbeidstilsynet.MeldingerReceiver.Domain.Data.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;

internal static class AltinnMappingExtensions
{
    public static PostMeldingRequest MapAltinnSummaryToPostMeldingRequest(
        this AltinnInstanceSummary altinnInstanceSummary,
        DateTime meldingReceivedAt
    )
    {
        return new PostMeldingRequest
        {
            MeldingId = altinnInstanceSummary.Metadata.InstanceGuid ?? Guid.NewGuid(),
            Source = MessageSource.Altinn,
            ApplicationReference =
                altinnInstanceSummary.Metadata.App
                ?? throw new ArgumentException(
                    "Could not find app name in metadata. This is required in order to succeed."
                ),
            MeldingReceivedAt = meldingReceivedAt,
            Metadata = altinnInstanceSummary.ToMetadataDictionary(),
            MainContent = altinnInstanceSummary.AltinnSkjema.ToUploadDocumentRequest(),
            Attachments = altinnInstanceSummary
                .Attachments.Select(attachment => attachment.ToUploadDocumentRequest())
                .ToList(),
        };
    }

    private static UploadDocumentRequest ToUploadDocumentRequest(this AltinnDocument altinnDocument)
    {
        return new UploadDocumentRequest
        {
            FileMetadata = altinnDocument.FileMetadata.ToDocumentMetadata(),
            InputStream = altinnDocument.DocumentContent,
            ScanResult = altinnDocument.FileMetadata.FileScanResult.MapToDocumentScanResult(),
        };
    }

    private static DocumentFileMetadata ToDocumentMetadata(this AltinnFileMetadata fileMetadata)
    {
        return new DocumentFileMetadata
        {
            ContentType = fileMetadata.ContentType ?? "application/octet-stream",
            FileName = fileMetadata.Filename ?? "unknown",
        };
    }

    private static DocumentScanResult MapToDocumentScanResult(this FileScanResult? fileScanResult)
    {
        return fileScanResult switch
        {
            FileScanResult.Clean => DocumentScanResult.Clean,
            FileScanResult.Infected => DocumentScanResult.Infected,
            _ => DocumentScanResult.Unknown,
        };
    }
}
