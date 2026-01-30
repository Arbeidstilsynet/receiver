using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Ports;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Arbeidstilsynet.Receiver.Implementation;

internal class ValkeyConsumer : IValkeyConsumer
{
    private readonly ILogger<ValkeyConsumer> _logger;
    private const string DefaultConsumerName = "consumer_1";
    private readonly IDatabase _db;

    public ValkeyConsumer(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<ValkeyConsumer> logger
    )
    {
        _logger = logger;
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<Dictionary<MessageId, Melding>> GetNotificationsAsync(
        ConsumerManifest consumerManifest
    )
    {
        const string streamName = IConstants.Stream.StreamName;
        var groupName = GetGroupName(consumerManifest);

        await _db.EnsureConsumerGroupExists(streamName, groupName);

        var results = await _db.StreamReadGroupAsync(
            key: streamName,
            groupName: groupName,
            consumerName: DefaultConsumerName,
            position: ">",
            count: 10
        );

        var (matchedEntries, excludedEntries) = results.GetMeldinger(
            consumerManifest,
            out var errors,
            _logger
        );

        if (errors.Count != 0)
        {
            _logger.LogError("Invalid messages consumed: [{errors}]", string.Join(", ", errors));
        }

        foreach (var messageId in excludedEntries)
        {
            // This is also acknowledging errors, to not handle them repeatedly
            await AcknowledgeMessageAsync(consumerManifest, messageId);
        }

        return matchedEntries;
    }

    public async Task<Dictionary<MessageId, Melding>> GetPendingMessagesAsync(
        ConsumerManifest consumerManifest
    )
    {
        var groupName = GetGroupName(consumerManifest);
        var streamEntries = await _db.StreamReadGroupAsync(
            key: IConstants.Stream.StreamName,
            groupName: groupName,
            consumerName: DefaultConsumerName,
            position: "0-0",
            count: 10
        );
        return streamEntries.GetMeldinger(consumerManifest, out _).MatchedEntries;
    }

    public Dictionary<MessageId, Melding> GetPendingMessages(ConsumerManifest consumerManifest)
    {
        var groupName = GetGroupName(consumerManifest);
        var streamEntries = _db.StreamReadGroup(
            key: IConstants.Stream.StreamName,
            groupName: groupName,
            consumerName: DefaultConsumerName,
            position: "0-0",
            count: 10
        );

        return streamEntries.GetMeldinger(consumerManifest, out _).MatchedEntries;
    }

    public Task<long> AcknowledgeMessageAsync(
        ConsumerManifest consumerManifest,
        MessageId messageId
    )
    {
        var groupName = GetGroupName(consumerManifest);
        return _db.StreamAcknowledgeAsync(IConstants.Stream.StreamName, groupName, messageId);
    }

    public async Task<long> AcknowledgeMessagesAsync(
        ConsumerManifest consumerManifest,
        IEnumerable<MessageId> messageIds
    )
    {
        var groupName = GetGroupName(consumerManifest);
        var messageIdsToAcknowledge = messageIds.ToArray();

        var ackCount = await _db.StreamAcknowledgeAsync(
            IConstants.Stream.StreamName,
            groupName,
            messageIdsToAcknowledge
        );

        return ackCount;
    }

    private static string GetGroupName(ConsumerManifest consumerManifest)
    {
        if (string.IsNullOrWhiteSpace(consumerManifest.ConsumerName))
        {
            throw new ArgumentException(
                "ConsumerManifest.ConsumerName must be set.",
                nameof(consumerManifest)
            );
        }

        return consumerManifest.ConsumerName;
    }
}

/// <summary>
/// Extensions for ValkeyConsumer
/// </summary>
file static class ValkeyConsumerExtensions
{
    /// <summary>
    /// Extracts and filters <see cref="Melding"/> objects from Redis stream entries.
    /// </summary>
    /// <param name="entries">The Redis stream entries to process.</param>
    /// <param name="consumerManifest">The consumer manifest used as a message filter.</param>
    /// <param name="errors">Outputs a list of message IDs that failed to deserialize or did not pass the filter.</param>
    /// <param name="logger">Optional logger used to report deserialization errors.</param>
    /// <returns>Pair of a dictionary of successfully filtered <see cref="Melding"/> objects keyed by their message ID and a list of excluded objects.</returns>
    public static (
        Dictionary<MessageId, Melding> MatchedEntries,
        List<MessageId> ExcludedEntries
    ) GetMeldinger(
        this IEnumerable<StreamEntry> entries,
        ConsumerManifest consumerManifest,
        out List<MessageId> errors,
        ILogger<ValkeyConsumer>? logger = null
    )
    {
        errors = [];
        List<MessageId> excludedMessages = [];

        var matches = new Dictionary<MessageId, Melding>();

        bool matchesConsumerManifest(Melding dto) =>
            consumerManifest.AppRegistrations is { Count: > 0 }
            && consumerManifest.AppRegistrations.Any(r => MatchesRegistration(r, dto));

        static bool MatchesRegistration(AppRegistration registration, Melding dto)
        {
            if (dto.Source != registration.MessageSource)
            {
                return false;
            }

            if (
                string.IsNullOrWhiteSpace(registration.AppId)
                || string.IsNullOrWhiteSpace(dto.ApplicationId)
            )
            {
                return false;
            }

            return string.Equals(
                dto.ApplicationId,
                registration.AppId,
                StringComparison.OrdinalIgnoreCase
            );
        }
        foreach (var entry in entries.ToDictionary(e => e.Id, e => e.GetMelding(logger)))
        {
            if (entry.Value is null)
            {
                errors.Add(entry.Key);
            }
            else if (matchesConsumerManifest(entry.Value))
            {
                matches.Add(entry.Key, entry.Value);
            }
            else
            {
                excludedMessages.Add(entry.Key);
            }
        }

        return (matches, excludedMessages);
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="Melding"/> object from a Redis stream entry.
    /// </summary>
    /// <param name="entry">The Redis stream entry containing the message data.</param>
    /// <param name="logger">Optional logger used to report deserialization errors.</param>
    /// <returns>The deserialized <see cref="Melding"/> object, or null if deserialization fails.</returns>
    private static Melding? GetMelding(
        this StreamEntry entry,
        ILogger<ValkeyConsumer>? logger = null
    )
    {
        var nameValueEntry = entry.Values.FirstOrDefault(v =>
            v.Name == IConstants.Stream.MessageKey
        );

        if (
            nameValueEntry == default
            || nameValueEntry.Value.ToString() is not { Length: > 0 } meldingJson
        )
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Melding>(meldingJson);
        }
        catch (Exception e)
        {
            logger?.LogError(
                e,
                "Could not process valkey message with id {ValkeyMessageId} and value {ValkeyValue}",
                entry.Id,
                meldingJson
            );
            return null;
        }
    }
}
