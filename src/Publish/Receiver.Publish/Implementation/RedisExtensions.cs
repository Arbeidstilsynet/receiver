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
    /// When maxConcurrency > 1, creates per-message DI scopes for parallel processing.
    /// When sequential, uses the provided consumer directly.
    /// </summary>
    public static async Task<(
        List<MessageId> SuccessfulMessages,
        List<RedriveException> UnsuccessfulMessages
    )> ConsumeNotifications(
        this IMeldingerConsumer consumer,
        IServiceScopeFactory scopeFactory,
        Dictionary<MessageId, Melding> notifications,
        int maxConcurrency,
        ApiMeters apiMeters,
        ILogger logger,
        bool triggeredFromRedrive = false
    )
    {
        using var rootActivity = ReceiverTracer.Source.StartActivity(ActivityKind.Consumer);
        var rootActivityId = rootActivity?.Id;

        var successfulMessages = new ConcurrentBag<MessageId>();
        var unsuccessfulMessages = new ConcurrentBag<RedriveException>();

        await Parallel.ForEachAsync(
            notifications,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
            async (kvp, ct) =>
            {
                var (messageId, melding) = kvp;
                // For parallel processing, each message gets its own scope and consumer.
                // For sequential, reuse the caller-provided consumer directly.
                var scope = maxConcurrency > 1 ? scopeFactory.CreateScope() : null;
                try
                {
                    var effectiveConsumer =
                        scope != null
                            ? scope.ServiceProvider.GetRequiredService<IMeldingerConsumer>()
                            : consumer;

                    var rootTraceParent = triggeredFromRedrive
                        ? rootActivityId
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
                        await effectiveConsumer.ConsumeMelding(melding);
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
                        logger.LogError(
                            e,
                            "Error consuming message with ID {MessageId}",
                            messageId
                        );
                    }
                }
                finally
                {
                    scope?.Dispose();
                }
            }
        );

        return (successfulMessages.ToList(), unsuccessfulMessages.ToList());
    }
}
