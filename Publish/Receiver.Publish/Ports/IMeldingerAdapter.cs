using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Meldinger.Receiver.Ports;

/// <summary>
/// Adapter for using <see cref="IMeldingerClient"/> methods.
/// </summary>
public interface IMeldingerAdapter
{
    /// <summary>
    /// Retrieves the main Altinn document for the specified melding.
    /// </summary>
    /// <typeparam name="T">The type of the document to retrieve.</typeparam>
    /// <param name="melding">The melding for which to retrieve the document.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the main Altinn document of type <typeparamref name="T"/>.</returns>
    Task<T> GetMainAltinnDocument<T>(Melding melding);
}
