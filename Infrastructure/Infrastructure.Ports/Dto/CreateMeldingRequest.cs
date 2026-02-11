using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

public record CreateMeldingRequest
{
    public required Guid Id { get; init; }
    public required MessageSource Source { get; init; }

    public required DateTime ReceivedAt { get; init; }

    public required string ApplicationId { get; init; }

    public Dictionary<string, string> Tags { get; init; } = [];
    public Dictionary<string, string> InternalTags { get; init; } = [];

    public DocumentStorageDto? MainDocumentData { get; init; }
    public DocumentStorageDto? StructuredData { get; init; }
    public required List<DocumentStorageDto> AttachmentData { get; init; }
}

public record DocumentStorageDto
{
    public required Guid DocumentId { get; init; }
    public required string InternalDocumentReference { get; init; }

    public required string ContentType { get; init; }
    public required string FileName { get; init; }

    public required DocumentScanResult ScanResult { get; set; }
    public Dictionary<string, string> Tags { get; set; } = [];
}
