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
                .Attachments.Select(attachment =>
                    attachment.ToUploadDocumentRequest(logger, sanitizeFilename: true)
                )
                .ToList(),
        };
    }

    private static UploadDocumentRequest ToUploadDocumentRequest(
        this AltinnDocument altinnDocument,
        ILogger logger,
        bool sanitizeFilename = false
    )
    {
        Guid? documentId =
            altinnDocument.FileMetadata.AltinnId == Guid.Empty
                ? null
                : altinnDocument.FileMetadata.AltinnId;
        if (sanitizeFilename)
        {
            documentId ??= Guid.NewGuid();
        }

        return new UploadDocumentRequest
        {
            DocumentId = documentId,
            FileMetadata = altinnDocument.FileMetadata.ToDocumentMetadata(
                sanitizeFilename ? documentId : null
            ),
            InputStream = altinnDocument.DocumentContent,
            ScanResult = altinnDocument.FileMetadata.FileScanResult.MapToDocumentScanResult(),
            Tags = altinnDocument.FileMetadata.ToDocumentTags(logger),
        };
    }

    private static UploadDocumentRequest AsClean(this UploadDocumentRequest uploadDocumentRequest)
    {
        return uploadDocumentRequest with { ScanResult = DocumentScanResult.Clean };
    }

    private static DocumentFileMetadata ToDocumentMetadata(
        this AltinnFileMetadata fileMetadata,
        Guid? sanitizedDocumentId
    )
    {
        var contentType =
            fileMetadata.ContentType
            ?? throw new ArgumentException("ContentType is required in order to succeed.");

        return new DocumentFileMetadata
        {
            ContentType = contentType,
            FileName = sanitizedDocumentId is { } documentId
                ? fileMetadata.GetSanitizedFilename(documentId, contentType)
                : fileMetadata.Filename
                    ?? throw new ArgumentException("Filename is required in order to succeed."),
        };
    }

    private static string GetSanitizedFilename(
        this AltinnFileMetadata fileMetadata,
        Guid documentId,
        string contentType
    )
    {
        var dataType = new string(
            (fileMetadata.AltinnDataType ?? "")
                .Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_')
                .ToArray()
        ).Trim('_');
        if (dataType.Length == 0)
        {
            dataType = "document";
        }

        return $"{dataType}_{documentId}{GetFileExtension(contentType)}";
    }

    private static string GetFileExtension(string contentType) =>
        contentType.Split(';', 2)[0].Trim().ToLowerInvariant() switch
        {
            "application/pdf" => ".pdf",
            "application/json" => ".json",
            "application/xml" or "text/xml" => ".xml",
            "text/plain" => ".txt",
            "text/csv" => ".csv",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/tiff" => ".tif",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "application/vnd.ms-powerpoint" => ".ppt",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
            "application/vnd.oasis.opendocument.text" => ".odt",
            "application/vnd.oasis.opendocument.spreadsheet" => ".ods",
            "application/vnd.oasis.opendocument.presentation" => ".odp",
            "application/zip" => ".zip",
            _ => "",
        };

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
