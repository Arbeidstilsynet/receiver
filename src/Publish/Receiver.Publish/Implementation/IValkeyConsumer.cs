global using MessageId = StackExchange.Redis.RedisValue;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Ports;

/// <summary>
/// Interface which can be dependency injected to use methods of <see cref="MeldingerReceiver"/>
/// </summary>
internal interface IValkeyConsumer
{
    /// <summary>
    /// Gets all notifications which have not been read yet for the consuming group.
    /// Creates a consuming group if it does not yet exist.
    /// </summary>
    /// <param name="consumerManifest">The consumer manifest. Used both for filtering messages and to create a consumer group.</param>
    /// <returns>A dictionary mapping message IDs to unread <see cref="Melding"/> objects.</returns>
    Task<Dictionary<MessageId, Melding>> GetNotificationsAsync(ConsumerManifest consumerManifest);

    /// <summary>
    /// Gets all notifications which have been read but not acknowledged yet for the consuming group.
    /// </summary>
    /// <param name="consumerManifest">The consumer manifest. Used both for filtering messages and to create a consumer group.</param>
    /// <returns>A dictionary mapping message IDs to pending <see cref="Melding"/> objects.</returns>
    Task<Dictionary<MessageId, Melding>> GetPendingMessagesAsync(ConsumerManifest consumerManifest);

    /// <summary>
    /// Gets all notifications which have been read but not acknowledged yet for the consuming group.
    /// </summary>
    /// <param name="consumerManifest">The consumer manifest. Used both for filtering messages and to create a consumer group.</param>
    /// <returns>A dictionary mapping message IDs to pending <see cref="Melding"/> objects.</returns>
    Dictionary<MessageId, Melding> GetPendingMessages(ConsumerManifest consumerManifest);

    /// <summary>
    /// Acknowledges a pending message to remove it from the pending messages list for the consuming group.
    /// The message will not be consumed again by the same group. When acknowledged by all groups, it can be deleted.
    /// </summary>
    /// <param name="consumerManifest">The consumer manifest.</param>
    /// <param name="messageId">The ID of the message to acknowledge.</param>
    /// <returns>The number of messages that have been acknowledged. Returns 1 if it has been successful in this case.</returns>
    Task<long> AcknowledgeMessageAsync(ConsumerManifest consumerManifest, MessageId messageId);

    /// <summary>
    /// Acknowledges a list of pending message to remove it from the pending messages list for the consuming group.
    /// The messages will not be consumed again by the same group. When acknowledged by all groups, it can be deleted.
    /// </summary>
    /// <param name="consumerManifest">The consumer manifest.</param>
    /// <param name="messageIds">The ID of the message to acknowledge.</param>
    /// <returns>The number of messages that have been acknowledged.</returns>
    Task<long> AcknowledgeMessagesAsync(
        ConsumerManifest consumerManifest,
        IEnumerable<MessageId> messageIds
    );
}
