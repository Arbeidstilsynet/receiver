using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Ports;

/// <summary>
/// Adapter for using <see cref="IMeldingerClient"/> methods.
/// </summary>
public interface IMeldingerAdapter
{
    /// <summary>
    /// Downloads and deserializes the structured data, if any, associated with the specified melding.
    /// </summary>
    /// <typeparam name="TStructuredData">The type of the structured data.</typeparam>
    /// <param name="melding">The melding for which to retrieve the document.</param>
    Task<TStructuredData?> FetchStructuredData<TStructuredData>(Melding melding);
}
