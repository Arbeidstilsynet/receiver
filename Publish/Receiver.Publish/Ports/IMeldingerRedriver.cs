using Arbeidstilsynet.Receiver.Ports.Model;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Ports;

/// <summary>
/// Provides methods for redriving (reprocessing) pending messages in the MeldingerReceiver system.
/// This interface can be dependency injected to trigger redrive operations for the consuming group.
/// </summary>
public interface IMeldingerRedriver
{
    /// <summary>
    /// Attempts to redrive (reprocess) all pending messages for the specified application and consumer group.
    /// This can be used to retry message delivery or processing for messages that have not been acknowledged.
    /// </summary>
    /// <returns>A redrive result which summarizes the operation.</returns>
    Task<RedriveResult> RedrivePendingMessages();

    /// <summary>
    /// Gets all pending messages for the specified application and consumer group.
    /// </summary>
    /// <returns>A dictionary which maps pending messages (ids) to a received melding.</returns>
    Task<Dictionary<string, Melding>> GetPendingMessages();

    /// <summary>
    /// Acknowledges all provided messages.
    /// </summary>
    /// <param name="messageIds">List of message Ids to acknowledge</param>
    /// <returns>Count of successfully acknowledged messages.</returns>
    Task<long> AcknowledgePendingMessages(List<MessageId> messageIds);
}
