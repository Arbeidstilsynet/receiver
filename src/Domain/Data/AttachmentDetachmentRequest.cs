namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public record AttachmentDetachmentRequest
{
    public required Guid DocumentId { get; init; }

    public bool Apply { get; init; }

    public string? ExpectedRevision { get; init; }
}
