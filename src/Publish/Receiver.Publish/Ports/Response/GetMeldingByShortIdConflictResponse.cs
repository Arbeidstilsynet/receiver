namespace Arbeidstilsynet.Receiver.Model.Response;

/// <summary>
/// Returned when a short id lookup matches more than one melding.
/// A short id is the trailing 12 hex characters of a melding GUID and is not guaranteed to be unique.
/// The caller can use <see cref="MatchingMeldingIds"/> to disambiguate by performing a full-id lookup.
/// </summary>
public record GetMeldingByShortIdConflictResponse
{
    /// <summary>
    /// The full melding GUIDs that share the requested short id.
    /// </summary>
    public required List<Guid> MatchingMeldingIds { get; init; }
}
