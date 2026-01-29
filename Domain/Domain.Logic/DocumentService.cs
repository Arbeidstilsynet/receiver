using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal class DocumentService(
    IMeldingRepository meldingRepository,
    IDocumentRepository documentRepository,
    IDocumentStorage documentStorage,
    ILogger<DocumentService> logger
) : IDocumentService
{
    public async Task<Document?> GetDocument(GetDocumentRequest request)
    {
        var melding = await meldingRepository.GetMeldingAsync(request.MeldingId);
        if (
            melding != null
            && (
                melding.ContentId == request.DocumentId
                || melding.AttachmentIds.Contains(request.DocumentId)
            )
        )
        {
            var document = await documentRepository.GetDocumentAsync(request.DocumentId);

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

        return null;
    }

    public async Task<IEnumerable<Document>?> GetAllDocuments(GetAllDocumentsRequest request)
    {
        var melding = await meldingRepository.GetMeldingAsync(request.MeldingId);
        if (melding != null)
        {
            var documents = await documentRepository.GetAllDocumentsForMelding(request.MeldingId);

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
        await documentStorage.Download(
            document,
            outputStream,
            cancellationToken: cancellationToken
        );
    }
}
