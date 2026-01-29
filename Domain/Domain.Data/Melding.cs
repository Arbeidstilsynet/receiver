namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public record Melding
{
    public required Guid Id { get; init; }
    public required MessageSource Source { get; init; }

    public required string ApplicationId { get; init; }

    public required DateTime ReceivedAt { get; init; }

    public Dictionary<string, string> Tags { get; init; } = []; // Fylt ut av avsender
    public Dictionary<string, string> InternalTags { get; init; } = []; // Fylles ut her i denne applikasjonen

    public Guid ContentId { get; init; }
    public List<Guid> AttachmentIds { get; init; } = [];
}
