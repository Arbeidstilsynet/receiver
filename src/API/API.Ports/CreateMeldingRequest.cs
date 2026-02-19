using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public record CreateMeldingRequest
{
    public required Guid MeldingId { get; init; }

    public required MessageSource Source { get; init; }

    public required string ApplicationReference { get; init; }

    public UploadDocumentRequest? MainContent { get; init; }

    public UploadDocumentRequest? StructuredData { get; init; }
    public List<UploadDocumentRequest> Attachments { get; init; } = [];
    public Dictionary<string, string> Metadata { get; init; } = new();
}
