using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

/// <summary>
/// Get content related to a Melding (main content or attachments)
/// </summary>
public interface IDocumentService
{
    public Task<Document?> GetDocument(
        GetDocumentRequest request,
        CancellationToken cancellationToken
    );

    public Task<IEnumerable<Document>?> GetAllDocuments(
        GetAllDocumentsRequest request,
        CancellationToken cancellationToken
    );

    public Task DownloadDocument(
        Document document,
        Stream outputStream,
        CancellationToken cancellationToken
    );
}
