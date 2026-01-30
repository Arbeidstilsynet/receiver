using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Model.Response;

namespace Arbeidstilsynet.Receiver.Ports;

/// <summary>
/// REST API client interface for interacting with the MeldingerReceiver application.
/// Provides methods to retrieve documents and document metadata associated with messages.
/// </summary>
public interface IMeldingerClient
{
    /// <summary>
    /// Retrieves metadata for a specific document associated with a message from the receiver application.
    /// </summary>
    /// <param name="meldingId">The unique identifier of the message.</param>
    /// <param name="documentId">The unique identifier of the document to retrieve.</param>
    /// <returns>A <see cref="Document"/> containing the document's metadata.</returns>
    Task<Document?> GetDocument(Guid meldingId, Guid documentId);

    /// <summary>
    /// Retrieves a specific document associated with a message from the receiver application.
    /// </summary>
    /// <param name="meldingId">The unique identifier of the message.</param>
    /// <param name="documentId">The unique identifier of the document to retrieve.</param>
    /// <returns>A <see cref="Stream"/> containing the document data.</returns>
    Task<Stream> DownloadDocument(Guid meldingId, Guid documentId);

    /// <summary>
    /// Retrieves metadata for all documents associated with a specific message from the receiver application.
    /// </summary>
    /// <param name="meldingId">The unique identifier of the message.</param>
    /// <returns>A <see cref="GetAllDocumentsResponse"/> containing metadata for all documents.</returns>
    Task<GetAllDocumentsResponse> GetDocuments(Guid meldingId);

    Task<ConsumerManifest> SubscribeConsumer(ConsumerManifest consumerManifest);
}
