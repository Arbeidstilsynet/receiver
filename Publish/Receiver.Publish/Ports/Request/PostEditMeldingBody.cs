using System.ComponentModel.DataAnnotations;

namespace Arbeidstilsynet.Receiver.Model.Request;

/// <summary>
/// Body for editing a melding. All properties are optional, and only the provided ones will be updated.
/// <br/>
///
/// </summary>
/// <remarks>Immediately deprecated because it's adhoc</remarks>
public record PostEditMeldingBody
{
    /// <summary>
    /// The ID of the melding to edit. This is required to identify which melding to update.
    /// </summary>
    [Required]
    public required Guid MeldingId { get; init; }

    /// <summary>
    /// The ID of the main content document. If provided, this will replace the existing main content. If not provided, the existing main content will remain unchanged.
    /// </summary>
    [Required]
    public required Guid NewMainContentId { get; init; }
}
