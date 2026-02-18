using System.ComponentModel.DataAnnotations;

namespace Arbeidstilsynet.Receiver.Model.Request;

/// <summary>
/// Body for editing a melding. Use it to update the main content of an existing melding by providing the melding ID and the new main content ID. This allows you to replace the main content of a melding without changing its metadata or attachments.
/// <br/>
///
/// </summary>
[Obsolete("Immediately deprecated because it's adhoc")]
public record PostEditMeldingBody
{
    /// <summary>
    /// The ID of the melding to edit. This is required to identify which melding to update.
    /// </summary>
    [Required]
    public required Guid MeldingId { get; init; }

    /// <summary>
    /// The ID of the main content document.
    /// </summary>
    [Required]
    public required Guid NewMainContentId { get; init; }
}
