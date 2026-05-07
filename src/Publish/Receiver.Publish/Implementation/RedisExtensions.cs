using System.Collections.Concurrent;
using System.Diagnostics;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Ports;
using Arbeidstilsynet.Receiver.Ports.Model;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="scopeFactory"></param>
    /// <param name="notifications"></param>
    /// <param name="maxConcurrency"></param>
    /// <param name="apiMeters"></param>
    /// <param name="logger"></param>
    /// <param name="triggeredFromRedrive"></param>
    /// <returns>Pair of successfully and unsucessfully processed message IDs.</returns>
    public static async Task<(
        List<MessageId> SuccessfulMessages,
        List<RedriveException> UnsuccessfulMessages
    )> ConsumeNotifications(
        this IServiceScopeFactory scopeFactory,
        Dictionary<MessageId, Melding> notifications,
        int maxConcurrency,
        ApiMeters apiMeters,
        ILogger logger,
        bool triggeredFromRedrive = false
    )
    {
        var successfulMessages = new ConcurrentBag<MessageId>();
        var unsuccessfulMessages = new ConcurrentBag<RedriveException>();
        using var rootActivity = ReceiverTracer.Source.StartActivity(ActivityKind.Consumer);

        await Parallel.ForEachAsync(
            notifications,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
            async (kvp, _) =>
            {
                var (messageId, melding) = kvp;
                using var scope = scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<IMeldingerConsumer>();

                using var activity = ReceiverTracer.Source.StartActivity();
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
                        new RedriveException()
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
            });

        return (successfulMessages.ToList(), unsuccessfulMessages.ToList());
    }
}
