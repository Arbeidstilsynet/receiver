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
        ApiMeters apiMeters,
        ILogger logger,
        bool triggeredFromRedrive = false
    )
    {
        var maxConcurrency = consumer.MaxConcurrency ?? 1;
        var successfulMessages = new ConcurrentBag<MessageId>();
        var unsuccessfulMessages = new ConcurrentBag<RedriveException>();

        await Parallel.ForEachAsync(
            notifications,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
            async (kvp, ct) =>
            {
                // For parallel processing, each message gets its own scope and consumer.
                // For sequential, reuse the caller-provided consumer directly.
                using var scope = scopeFactory.CreateScope();
                var effectiveConsumer =
                    maxConcurrency > 1
                        ? scope.ServiceProvider.GetRequiredService<IMeldingerConsumer>()
                        : consumer;
                await effectiveConsumer.ConsumeMelding(
                    kvp,
                    scope.ServiceProvider,
                    successfulMessages,
                    unsuccessfulMessages,
                    apiMeters,
                    logger,
                    triggeredFromRedrive
                );
            }
        );

        return (successfulMessages.ToList(), unsuccessfulMessages.ToList());
    }

    private static async Task ConsumeMelding(
        this IMeldingerConsumer consumer,
        KeyValuePair<MessageId, Melding> meldingToConsumePair,
        IServiceProvider serviceProvider,
        ConcurrentBag<MessageId> successfulMessages,
        ConcurrentBag<RedriveException> unsuccessfulMessages,
        ApiMeters apiMeters,
        ILogger logger,
        bool triggeredFromRedrive = false
    )
    {
        using var rootActivity = ReceiverTracer.Source.StartActivity(ActivityKind.Consumer);
        var rootActivityId = rootActivity?.Id;
        var (messageId, melding) = meldingToConsumePair;
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
            var meldingerClient = serviceProvider.GetRequiredService<IMeldingerClient>();
            var current =
                await meldingerClient.GetMelding(melding.Id)
                ?? throw new InvalidOperationException(
                    $"Melding {melding.Id} no longer exists in receiver."
                );
            if (
                current.Id != melding.Id
                || current.ApplicationId != melding.ApplicationId
                || current.Source != melding.Source
            )
            {
                throw new InvalidOperationException(
                    $"Receiver identity changed for melding {melding.Id}."
                );
            }

            melding = current;
            var consumedAt = DateTime.Now;
            apiMeters.MeldingConsumed(melding, triggeredFromRedrive);
            await consumer.ConsumeMelding(melding);
            successfulMessages.Add(messageId);

            // update metrics
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
            var rootTraceId = meldingToConsumePair.Value.GetInternalTag("rootTraceId");
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
    }
}
