using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public record PostMeldingRequest
{
    public required Guid MeldingId { get; init; }

    public required MessageSource Source { get; init; }

    public required string ApplicationReference { get; init; }

    public required DateTime MeldingReceivedAt { get; init; }

    /// <summary>
    /// The main content of the melding. This is typically the document that contains the core information about the melding, such as a PDF form or a JSON payload. This field is optional, as some <see cref="Melding"/>s may only have structured data.
    /// </summary>
    public UploadDocumentRequest? MainContent { get; init; }

    /// <summary>
    /// Structured data related to the melding. This is typically a JSON payload that contains structured information about the melding, such as form data or metadata. This field is optional, as some <see cref="Melding"/>s may only have a main content document.
    /// </summary>
    public UploadDocumentRequest? StructuredData { get; init; }
    public List<UploadDocumentRequest> Attachments { get; init; } = [];
    public Dictionary<string, string> Metadata { get; init; } = new();
}
