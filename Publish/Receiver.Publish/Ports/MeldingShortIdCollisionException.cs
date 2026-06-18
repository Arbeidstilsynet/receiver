namespace Arbeidstilsynet.Receiver.Model;

/// <summary>
/// Thrown by <see cref="IMeldingerClient.GetMeldingByShortId"/> when a short id matches more than one melding.
/// A short id (the trailing 12 hex characters of a melding GUID) is not guaranteed to be unique.
/// </summary>
public class MeldingShortIdCollisionException : Exception
{
    /// <summary>
    /// Creates a new <see cref="MeldingShortIdCollisionException"/>.
    /// </summary>
    /// <param name="shortId">The ambiguous short id.</param>
    /// <param name="matchingMeldingIds">The full melding GUIDs that share the short id.</param>
    public MeldingShortIdCollisionException(string shortId, IReadOnlyList<Guid> matchingMeldingIds)
        : base(
            $"The short id '{shortId}' matches {matchingMeldingIds.Count} meldinger. Use the full melding id to disambiguate."
        )
    {
        ShortId = shortId;
        MatchingMeldingIds = matchingMeldingIds;
    }

    /// <summary>
    /// The ambiguous short id that was looked up.
    /// </summary>
    public string ShortId { get; }

    /// <summary>
    /// The full melding GUIDs that share the requested short id.
    /// </summary>
    public IReadOnlyList<Guid> MatchingMeldingIds { get; }
}
