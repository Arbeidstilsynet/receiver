namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public record AttachmentDetachmentResponse
{
    public required Guid MeldingId { get; init; }

    public required Guid DocumentId { get; init; }

    public required string ApplicationId { get; init; }

    public required Guid? MainContentId { get; init; }

    public required Guid? StructuredDataId { get; init; }

    public required List<Guid> AttachmentIdsBefore { get; init; }

    public required List<Guid> AttachmentIdsAfter { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required string Revision { get; init; }

    public required bool Applied { get; init; }

    public required bool AlreadyDetached { get; init; }
}
