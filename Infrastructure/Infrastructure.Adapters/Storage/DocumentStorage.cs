using System.Net;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Storage;

internal class DocumentStorage(
    StorageClient client,
    IInternalDocumentRepository internalDocumentRepository,
    ILogger<DocumentStorage> logger,
    IOptions<InfrastructureConfiguration> options
) : IDocumentStorage
{
    private readonly DocumentStorageConfiguration _config = options
        .Value
        .DocumentStorageConfiguration;

    public async Task<UploadResponse> Upload(
        UploadRequest request,
        CancellationToken cancellationToken
    )
    {
        var bucketName = _config.BucketName;
        // Upload file

        await using (request.InputStream)
        {
            var uploadedObject = await client.UploadObjectAsync(
                bucketName,
                request.CreateGcsObjectName(),
                null, // contentType must be null in order to avoid buffering the content (https://cloud.google.com/storage/docs/streaming-uploads)
                request.InputStream,
                cancellationToken: cancellationToken
            );
            logger.LogInformation("uploaded document with id {Id}", uploadedObject.Id);
            return new UploadResponse
            {
                PersistedDocument = new DocumentStorageDto
                {
                    DocumentId = request.Document.DocumentId,
                    ContentType = request.Document.FileMetadata.ContentType,
                    FileName = request.Document.FileMetadata.FileName,
                    InternalDocumentReference = uploadedObject.Name,
                    ScanResult = request.Document.ScanResult,
                    Tags = request.Document.Tags,
                },
            };
        }
    }

    public async Task Download(
        Document document,
        Stream outputStream,
        CancellationToken cancellationToken
    )
    {
        var internalDocumentReference =
            await internalDocumentRepository.GetInternalDocumentReferenceAsync(
                document.DocumentId,
                cancellationToken
            )
            ?? throw new InvalidOperationException(
                $"Document with ID {document.DocumentId} not found. This might happen if the document ID is invalid or the document does not exist in the repository. Please verify the document ID and ensure it is correct."
            );
        await Download(internalDocumentReference, outputStream, cancellationToken);
    }

    public async Task Download(
        string internalDocumentId,
        Stream outputStream,
        CancellationToken cancellationToken
    )
    {
        var bucketName = _config.BucketName;
        try
        {
            await client.DownloadObjectAsync(
                bucketName,
                internalDocumentId,
                outputStream,
                cancellationToken: cancellationToken
            );
        }
        catch (GoogleApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation(
                "Document with reference {Reference} not found in bucket {BucketName}",
                internalDocumentId,
                bucketName
            );
            throw;
        }
    }
}
