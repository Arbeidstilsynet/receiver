using Arbeidstilsynet.Common.Altinn.Extensions;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using AltinnFileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;
using DocumentFileMetadata = Arbeidstilsynet.MeldingerReceiver.Domain.Data.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.App.Extensions;

internal static class AltinnMappingExtensions
{
    public static CreateMeldingRequest MapAltinnSummaryToPostMeldingRequest(
        this AltinnInstanceSummary altinnInstanceSummary,
        ILogger logger
    )
    {
        return new CreateMeldingRequest
        {
            MeldingId = altinnInstanceSummary.Metadata.InstanceGuid,
            Source = MessageSource.Altinn,
            ApplicationReference =
                altinnInstanceSummary.Metadata.App
                ?? throw new ArgumentException(
                    "Could not find app name in metadata. This is required in order to succeed."
                ),
            Metadata = altinnInstanceSummary.ToMetadataDictionary(),
            MainContent = altinnInstanceSummary
                .SkjemaAsPdf.ToUploadDocumentRequest(logger)
                .AsClean(),
            StructuredData = altinnInstanceSummary
                .StructuredData?.ToUploadDocumentRequest(logger)
                .AsClean(),
            Attachments = altinnInstanceSummary
                .Attachments.Select(attachment => attachment.ToUploadDocumentRequest(logger))
                .ToList(),
        };
    }

    private static UploadDocumentRequest ToUploadDocumentRequest(
        this AltinnDocument altinnDocument,
        ILogger logger
    )
    {
        return new UploadDocumentRequest
        {
            DocumentId =
                altinnDocument.FileMetadata.AltinnId == Guid.Empty
                    ? null
                    : altinnDocument.FileMetadata.AltinnId,
            FileMetadata = altinnDocument.FileMetadata.ToDocumentMetadata(),
            InputStream = altinnDocument.DocumentContent,
            ScanResult = altinnDocument.FileMetadata.FileScanResult.MapToDocumentScanResult(),
            Tags = altinnDocument.FileMetadata.ToDocumentTags(logger),
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
            ContentType =
                fileMetadata.ContentType
                ?? throw new ArgumentException("ContentType is required in order to succeed."),
            FileName =
                fileMetadata.Filename
                ?? throw new ArgumentException("Filename is required in order to succeed."),
        };
    }

    private static Dictionary<string, string> ToDocumentTags(
        this AltinnFileMetadata fileMetadata,
        ILogger logger
    )
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

        foreach (var (key, value) in fileMetadata.Metadata)
        {
            if (!tags.TryAdd(key, value) && tags[key] != value)
            {
                logger.LogWarning(
                    "Ignoring conflicting Altinn file metadata key {MetadataKey} for document {DocumentId}; the canonical tag value is retained.",
                    key,
                    fileMetadata.AltinnId
                );
            }
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
