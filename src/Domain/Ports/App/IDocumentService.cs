using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;

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

    /// <summary>
    /// Lists soft-deleted attachments on a melding. Returns null when the melding does not exist.
    /// </summary>
    public Task<IEnumerable<Document>?> GetDeletedDocuments(
        GetAllDocumentsRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Soft-deletes or restores an attachment. Returns false when the document does not exist on
    /// the melding. Throws <see cref="Data.Exceptions.DocumentDeletionNotAllowedException"/> for
    /// main content and structured data.
    /// </summary>
    public Task<bool> SetDocumentDeleted(
        Guid meldingId,
        Guid documentId,
        bool deleted,
        CancellationToken cancellationToken
    );

    public Task DownloadDocument(
        Document document,
        Stream outputStream,
        CancellationToken cancellationToken
    );
}
