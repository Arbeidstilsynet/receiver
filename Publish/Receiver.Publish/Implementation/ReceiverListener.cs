using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Arbeidstilsynet.Receiver.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Arbeidstilsynet.Receiver.Implementation;

internal class ReceiverListener(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var valkey = scope.ServiceProvider.GetRequiredService<IValkeyConsumer>();
        var consumer = scope.ServiceProvider.GetRequiredService<IMeldingerConsumer>();
        var connectionMultiplexer =
            scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReceiverListener>>();
        var apiMeters = scope.ServiceProvider.GetRequiredService<ApiMeters>();
        var meldingerClient = scope.ServiceProvider.GetRequiredService<IMeldingerClient>();

        CheckIfConsumerManifestIsValid(consumer.ConsumerManifest);

        try
        {
            await meldingerClient.SubscribeConsumer(consumer.ConsumerManifest);
            logger.LogInformation(
                "Consumer {ConsumerName} was successfully registered for consumer. Listening to the following apps: {AppRegistrations}",
                consumer.ConsumerManifest.ConsumerName,
                consumer.ConsumerManifest.AppRegistrations
            );
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Could not connect subscribe to receiver backend. Is the receiver backend available?"
            );
        }

        var _ = apiMeters._meter.CreateObservableUpDownCounter(
            "meldinger_consumer_pending_melding_count",
            () =>
                new Measurement<long>(
                    valkey.GetPendingMessages(consumer.ConsumerManifest).Count,
                    new KeyValuePair<string, object?>(
                        "consumerName",
                        consumer.ConsumerManifest.ConsumerName
                    )
                ),
            "meldinger",
            "Counts the number of pending meldinger"
        );

        var channel = await CreateSubscriptionWriter(connectionMultiplexer, logger);

        // Start by getting up to date
        await ReadFromStreamAndTriggerConsumer(consumer, valkey, apiMeters, logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var listeningTimeout = CancellationTokenSource.CreateLinkedTokenSource(
                    stoppingToken
                );
                listeningTimeout.CancelAfter(consumer.PollInterval ?? 10 * 60 * 1000);

                await foreach (var message in channel.Reader.ReadAllAsync(listeningTimeout.Token))
                {
                    logger.LogInformation(
                        "Received notification for message ID: {MessageId}",
                        message
                    );
                    await ReadFromStreamAndTriggerConsumer(consumer, valkey, apiMeters, logger);
                }
            }
            catch (OperationCanceledException canceledException)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    // Timeout occurred, check for pending messages
                    await ReadFromStreamAndTriggerConsumer(consumer, valkey, apiMeters, logger);
                }
                else
                {
                    logger.LogInformation(
                        canceledException,
                        "ReceiverListener job is stopping as cancellation was requested."
                    );
                    break;
                }
            }
            catch (Exception e)
            {
                logger.LogError(
                    e,
                    "ReceiverListener job stopped unexpectedly. Is the receiver stream (valkey) available?"
                );
                break;
            }
        }

        channel.Writer.Complete();
    }

    public static void CheckIfConsumerManifestIsValid(ConsumerManifest consumerManifest)
    {
        if (
            string.IsNullOrWhiteSpace(consumerManifest.ConsumerName)
            || consumerManifest.AppRegistrations is not { Count: > 0 }
        )
        {
            throw new ArgumentException(
                "ConsumerManifest must have ConsumerName and at least one AppRegistration.",
                nameof(consumerManifest)
            );
        }

        var invalidAltinnAppRegistrations = consumerManifest
            .AppRegistrations.Where(a =>
                a.MessageSource == MessageSource.Altinn && (a.AppId?.Contains('/') ?? false)
            )
            .ToList();

        if (invalidAltinnAppRegistrations.Count > 0)
        {
            throw new ArgumentException(
                $"Altinn registrations with invalid id detected: {string.Join(" ", invalidAltinnAppRegistrations.Select(a => a.AppId))}. Remove the Altinn org identifier here."
            );
        }
    }

    private async Task ReadFromStreamAndTriggerConsumer(
        IMeldingerConsumer consumer,
        IValkeyConsumer valkey,
        ApiMeters apiMeters,
        ILogger<ReceiverListener> logger
    )
    {
        using var activity = ReceiverTracer.Source.StartActivity();

        var totalNotifications = 0;
        var totalAcknowledged = 0;

        var notifications = await valkey.GetNotificationsAsync(consumer.ConsumerManifest);
        totalNotifications += notifications.Count;

        var (messageIdsToAcknowledge, _) = await consumer.ConsumeNotifications(
            notifications,
            apiMeters,
            logger
        );

        if (messageIdsToAcknowledge.Count > 0)
        {
            _ = await valkey.AcknowledgeMessagesAsync(
                consumer.ConsumerManifest,
                messageIdsToAcknowledge
            );
            totalAcknowledged += messageIdsToAcknowledge.Count;
        }

        if (totalNotifications > 0)
        {
            logger.LogInformation(
                "We have received {NotificationCount} notifications, where {ProcessedNotificationCount} was handled by a consumer.",
                totalNotifications,
                totalAcknowledged
            );
        }
    }

    private async Task<Channel<RedisValue>> CreateSubscriptionWriter(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<ReceiverListener> logger
    )
    {
        var sub = connectionMultiplexer.GetSubscriber();
        var channel = Channel.CreateUnbounded<RedisValue>(
            new UnboundedChannelOptions() { SingleWriter = false, SingleReader = true }
        );

        await sub.SubscribeAsync(
            new RedisChannel(IConstants.Stream.StreamName, RedisChannel.PatternMode.Literal),
            (_, meldingId) =>
            {
                if (!channel.Writer.TryWrite(meldingId))
                {
                    logger.LogError(
                        "Failed to write message to channel. MessageId: <{MessageId}>",
                        meldingId
                    );
                }
            }
        );

        return channel;
    }
}

internal static class ReceiverTracer
{
    public static readonly ActivitySource Source = new("AT.Common.MeldingerReceiver");
}
