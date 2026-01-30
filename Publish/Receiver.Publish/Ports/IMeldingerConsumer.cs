using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Ports;

/// <summary>
/// Defines the contract for a consumer application that receives and processes notifications from MeldingerReceiver.
/// Implement this interface to handle new notifications and configure consumer-specific settings.
/// </summary>
public interface IMeldingerConsumer
{
    /// <summary>
    /// Gets the unique application identifier for this consumer.
    /// This should match the application ID used when sending or receiving messages.
    /// </summary>
    public ConsumerManifest ConsumerManifest { get; }

    /// <summary>
    /// Gets the polling interval (in milliseconds) for checking new notifications.
    /// We use pub/sub for sending and receiving messages, so this job runs only as a backup and hence does not need to run high frequently.
    /// If null, the default interval will be used by the receiver (1h).
    /// </summary>
    public int? PollInterval { get; }

    /// <summary>
    /// Called when new <see cref="Melding"/> are available for this consumer.
    /// Throws an exception when a message could not be consumed.
    /// If an exception is thrown, the message may be re-driven later.
    /// </summary>
    /// <param name="newMelding">A thus far unhandled <see cref="Melding"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task ConsumeMelding(Melding newMelding);
}
