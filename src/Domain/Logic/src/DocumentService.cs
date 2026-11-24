using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Extensions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal class DocumentService(
    IMeldingRepository meldingRepository,
    IDocumentRepository documentRepository,
    IDocumentStorage documentStorage,
    ILogger<DocumentService> logger
) : IDocumentService
{
    public async Task<Document?> GetDocument(
        GetDocumentRequest request,
        CancellationToken cancellationToken
    )
    {
        var melding = await meldingRepository.GetMelding(request.MeldingId, cancellationToken);

        if (melding == null || !melding.ContainsDocument(request.DocumentId))
            return null;

        var document = await documentRepository.GetDocumentAsync(
            request.DocumentId,
            cancellationToken
        );

        if (document == null)
        {
            return null;
        }

        if (!document.IsDocumentSafeToUse)
        {
            throw new DocumentNotSafeToUseException(document);
        }
        return document;
    }

    public async Task<IEnumerable<Document>?> GetAllDocuments(
        GetAllDocumentsRequest request,
        CancellationToken cancellationToken
    )
    {
        var melding = await meldingRepository.GetMelding(request.MeldingId, cancellationToken);
        if (melding == null)
        {
            return null;
        }

        var documents = await documentRepository.GetAllDocumentsForMelding(
            request.MeldingId,
            cancellationToken
        );
        return ExcludeUnsafeDocuments(request.MeldingId, documents);
    }

    public async Task<IEnumerable<Document>?> GetDeletedDocuments(
        GetAllDocumentsRequest request,
        CancellationToken cancellationToken
    )
    {
        var melding = await meldingRepository.GetMelding(request.MeldingId, cancellationToken);
        if (melding == null)
        {
            return null;
        }

        var documents = await documentRepository.GetDeletedDocumentsForMelding(
            request.MeldingId,
            cancellationToken
        );
        return ExcludeUnsafeDocuments(request.MeldingId, documents);
    }

    public async Task<bool> SetDocumentDeleted(
        Guid meldingId,
        Guid documentId,
        bool deleted,
        CancellationToken cancellationToken
    )
    {
        var found = await documentRepository.SetDeleted(
            meldingId,
            documentId,
            deleted,
            cancellationToken
        );
        if (found)
        {
            logger.LogInformation(
                "Set deleted to {Deleted} for document {DocumentId} on melding {MeldingId}",
                deleted,
                documentId,
                meldingId
            );
        }
        return found;
    }

    private List<Document> ExcludeUnsafeDocuments(Guid meldingId, List<Document> documents)
    {
        var unsafeDocuments = documents.Where(d => !d.IsDocumentSafeToUse).ToList();
        if (unsafeDocuments.Count > 0)
        {
            logger.LogSkippedUnsafeDocuments(meldingId, unsafeDocuments);
        }
        return documents.Where(d => d.IsDocumentSafeToUse).ToList();
    }

    public async Task DownloadDocument(
        Document document,
        Stream outputStream,
        CancellationToken cancellationToken
    )
    {
        await documentStorage.Download(document, outputStream, cancellationToken);
    }
}
