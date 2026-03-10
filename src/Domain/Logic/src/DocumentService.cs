using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Extensions;
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
        if (melding != null)
        {
            var documents = await documentRepository.GetAllDocumentsForMelding(
                request.MeldingId,
                cancellationToken
            );

            var unsafeDocuments = new List<Document>();
            var safeDocuments = new List<Document>();

            foreach (var document in documents)
            {
                if (!document.IsDocumentSafeToUse)
                {
                    unsafeDocuments.Add(document);
                }
                else
                {
                    safeDocuments.Add(document);
                }
            }

            if (unsafeDocuments.Count > 0)
            {
                logger.LogSkippedUnsafeDocuments(request.MeldingId, unsafeDocuments);
            }

            return safeDocuments;
        }
        else
        {
            return null;
        }
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
