namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

/// <summary>
/// Represents a message (melding) received by the system.
/// </summary>
public record Melding
{
    public required Guid Id { get; init; }
    public required MessageSource Source { get; init; }

    public required string ApplicationId { get; init; }

    public required DateTime ReceivedAt { get; init; }

    public Dictionary<string, string> Tags { get; init; } = []; // Fylt ut av avsender
    public Dictionary<string, string> InternalTags { get; init; } = []; // Fylles ut her i denne applikasjonen

    /// <summary>
    /// The ID of the main content document associated with this melding. This is typically the document that contains the core information about the melding, such as a PDF form. This field is optional, as some <see cref="Melding"/>s may only have <see cref="Melding.StructuredDataId"/>.
    /// </summary>
    public Guid? MainContentId { get; init; }
    
    /// <summary>
    /// The ID of the structured data document associated with this melding. This is typically a JSON payload. This field is optional, as some <see cref="Melding"/>s may only have a main content document.
    /// </summary>
    public Guid? StructuredDataId { get; init; }
    
    /// <summary>
    /// A list of IDs of attachment documents associated with this melding. These are typically additional documents that provide supplementary information about the melding. This field is optional, as some <see cref="Melding"/>s may not have any attachments.
    /// </summary>
    public List<Guid> AttachmentIds { get; init; } = [];
}
