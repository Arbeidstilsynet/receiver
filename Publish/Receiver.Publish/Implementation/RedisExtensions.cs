using System.Diagnostics;
using Arbeidstilsynet.Receiver.Ports;
using Arbeidstilsynet.Receiver.Ports.Model;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.Receiver.Implementation;

internal static class RedisExtensions
{
    public static async Task EnsureConsumerGroupExists(
        this StackExchange.Redis.IDatabase database,
        string streamName,
        string groupName
    )
    {
        if (
            !await database.KeyExistsAsync(streamName)
            || (await database.StreamGroupInfoAsync(streamName)).All(x => x.Name != groupName)
        )
        {
            await database.StreamCreateConsumerGroupAsync(streamName, groupName, "0-0");
        }
    }

    /// <summary>
    /// Consumes notifications from a Redis stream and processes them using the provided consumer.
    /// </summary>
    /// <param name="consumer"></param>
    /// <param name="notifications"></param>
    /// <param name="apiMeters"></param>
    /// <param name="logger"></param>
    /// <param name="triggeredFromRedrive"></param>
    /// <returns>Pair of successfully and unsucessfully processed message IDs.</returns>
    public static async Task<(
        List<MessageId> SuccessfulMessages,
        List<RedriveException> UnsuccessfulMessages
    )> ConsumeNotifications(
        this IMeldingerConsumer consumer,
        Dictionary<MessageId, Melding> notifications,
        ApiMeters apiMeters,
        ILogger logger,
        bool triggeredFromRedrive = false
    )
    {
        using var rootActivity = ReceiverTracer.Source.StartActivity(ActivityKind.Consumer);
        var successfulMessages = new List<MessageId>();
        var unsuccessfulMessages = new List<RedriveException>();

        foreach (var (messageId, melding) in notifications)
        {
            var rootTraceParent = triggeredFromRedrive
                ? rootActivity?.Id
                : melding.GetInternalTag("rootTraceParent");
            using var activity = ReceiverTracer.Source.StartActivity(
                $"Consume {melding.ApplicationId} Notification",
                ActivityKind.Internal,
                rootTraceParent
            );
            try
            {
                var consumedAt = DateTime.Now;
                apiMeters.MeldingConsumed(melding, triggeredFromRedrive);
                await consumer.ConsumeMelding(melding);
                successfulMessages.Add(messageId);
                apiMeters.MeldingAcknowledged(melding, triggeredFromRedrive);
                apiMeters.RegisterMeldingDurationFromStart(melding, triggeredFromRedrive);
                apiMeters.RegisterMeldingDurationFromConsumerHook(
                    melding,
                    consumedAt,
                    triggeredFromRedrive
                );
            }
            catch (Exception e)
            {
                var rootTraceId = melding.GetInternalTag("rootTraceId");
                unsuccessfulMessages.Add(
                    new()
                    {
                        ValkeyMessageId = messageId,
                        MeldingId = melding.Id,
                        ExceptionMessage = e.Message,
                        TraceId = activity?.TraceId.ToString(),
                        OriginalTraceId = rootTraceId,
                    }
                );
                logger.LogError(e, "Error consuming message with ID {MessageId}", messageId);
            }
        }

        return (successfulMessages, unsuccessfulMessages);
    }
}
