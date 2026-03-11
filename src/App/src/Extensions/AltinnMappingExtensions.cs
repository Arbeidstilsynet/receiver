using Arbeidstilsynet.Common.Altinn.Extensions;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using AltinnFileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;
using DocumentFileMetadata = Arbeidstilsynet.MeldingerReceiver.Domain.Data.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.App.Extensions;

internal static class AltinnMappingExtensions
{
    public static CreateMeldingRequest MapAltinnSummaryToPostMeldingRequest(
        this AltinnInstanceSummary altinnInstanceSummary
    )
    {
        return new CreateMeldingRequest
        {
            MeldingId = altinnInstanceSummary.Metadata.InstanceGuid ?? Guid.NewGuid(),
            Source = MessageSource.Altinn,
            ApplicationReference =
                altinnInstanceSummary.Metadata.App
                ?? throw new ArgumentException(
                    "Could not find app name in metadata. This is required in order to succeed."
                ),
            Metadata = altinnInstanceSummary.ToMetadataDictionary(),
            MainContent = altinnInstanceSummary.SkjemaAsPdf.ToUploadDocumentRequest().AsClean(),
            StructuredData = altinnInstanceSummary
                .StructuredData?.ToUploadDocumentRequest()
                .AsClean(),
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
            Tags = altinnDocument.FileMetadata.ToDocumentTags(),
        };
    }

    private static UploadDocumentRequest AsClean(this UploadDocumentRequest uploadDocumentRequest)
    {
        return uploadDocumentRequest with { ScanResult = DocumentScanResult.Clean };
    }

    private static DocumentFileMetadata ToDocumentMetadata(this AltinnFileMetadata fileMetadata)
    {
        return new DocumentFileMetadata
        {
            ContentType = fileMetadata.ContentType ?? "application/octet-stream",
            FileName = fileMetadata.Filename ?? "unknown",
        };
    }

    private static Dictionary<string, string> ToDocumentTags(this AltinnFileMetadata fileMetadata)
    {
        var tags = new Dictionary<string, string>();

        if (fileMetadata.AltinnId != Guid.Empty)
        {
            tags.Add("AltinnId", fileMetadata.AltinnId.ToString());
        }

        if (fileMetadata.AltinnDataType is { Length: > 0 } dataType)
        {
            tags.Add("AltinnDataType", dataType);
        }

        return tags;
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
