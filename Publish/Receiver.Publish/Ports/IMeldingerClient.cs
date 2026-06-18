using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Model;
using Arbeidstilsynet.Receiver.Model.Response;

namespace Arbeidstilsynet.Receiver.Ports;

/// <summary>
/// REST API client interface for interacting with the MeldingerReceiver application.
/// Provides methods to retrieve documents and document metadata associated with messages.
/// </summary>
public interface IMeldingerClient
{
    /// <summary>
    /// Retrieves a specific <see cref="Melding"/> from the receiver application using its unique identifier.
    /// </summary>
    /// <param name="meldingId"></param>
    /// <returns>A <see cref="Melding"/> object if found; otherwise, null.</returns>
    Task<Melding?> GetMelding(Guid meldingId);

    /// <summary>
    /// Retrieves a <see cref="Melding"/> by its short id, i.e. the trailing 12 hex characters of the
    /// melding GUID (the last GUID segment).
    /// </summary>
    /// <param name="shortId">The trailing 12 hex characters of the melding GUID.</param>
    /// <returns>The matching <see cref="Melding"/> if exactly one is found; otherwise, null.</returns>
    /// <exception cref="MeldingShortIdCollisionException">
    /// Thrown when more than one melding shares the given short id. The full ids are available on the exception.
    /// </exception>
    Task<Melding?> GetMeldingByShortId(string shortId);

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

    /// <summary>
    /// Subscribes a consumer to receive messages from the MeldingerReceiver application.
    /// </summary>
    /// <param name="consumerManifest"></param>
    /// <returns></returns>
    Task<ConsumerManifest> SubscribeConsumer(ConsumerManifest consumerManifest);
}
