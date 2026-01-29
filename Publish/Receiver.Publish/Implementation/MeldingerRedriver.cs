using Arbeidstilsynet.Meldinger.Receiver.Ports;
using Arbeidstilsynet.Meldinger.Receiver.Ports.Model;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.Meldinger.Receiver.Implementation;

internal class MeldingerRedriver(
    IValkeyConsumer valkeyConsumer,
    IMeldingerConsumer meldingerConsumer,
    ApiMeters apiMeters,
    ILogger<MeldingerRedriver> logger
) : IMeldingerRedriver
{
    public async Task<long> AcknowledgePendingMessages(List<MessageId> messageIds)
    {
        if (meldingerConsumer.ConsumerManifest.AppRegistrations is not { Count: > 0 })
        {
            return 0;
        }

        if (messageIds.Count == 0)
        {
            return 0;
        }

        return await valkeyConsumer.AcknowledgeMessagesAsync(
            meldingerConsumer.ConsumerManifest,
            messageIds
        );
    }

    public async Task<Dictionary<string, Melding>> GetPendingMessages()
    {
        if (meldingerConsumer.ConsumerManifest.AppRegistrations is not { Count: > 0 })
        {
            return new Dictionary<string, Melding>();
        }

        var pending = await valkeyConsumer.GetPendingMessagesAsync(
            meldingerConsumer.ConsumerManifest
        );
        return pending.ToDictionary(kvp => (string)kvp.Key!, kvp => kvp.Value);
    }

    public async Task<RedriveResult> RedrivePendingMessages()
    {
        if (meldingerConsumer.ConsumerManifest.AppRegistrations is not { Count: > 0 })
        {
            logger.LogInformation("No pending messages to redrive.");
            return new()
            {
                SuccessfulCount = 0,
                UnsuccessfulCount = 0,
                RedriveExceptions = new(),
            };
        }

        var pendingMessages = await valkeyConsumer.GetPendingMessagesAsync(
            meldingerConsumer.ConsumerManifest
        );

        var (successfulMessages, unsuccessfulMessages) =
            await meldingerConsumer.ConsumeNotifications(pendingMessages, apiMeters, logger, true);

        if (successfulMessages.Count != 0)
        {
            _ = await valkeyConsumer.AcknowledgeMessagesAsync(
                meldingerConsumer.ConsumerManifest,
                successfulMessages
            );

            logger.LogInformation(
                "Redrove {Count} pending messages for consumer {ConsumerName}",
                successfulMessages.Count,
                meldingerConsumer.ConsumerManifest.ConsumerName
            );
        }
        return new()
        {
            SuccessfulCount = successfulMessages.Count,
            UnsuccessfulCount = unsuccessfulMessages.Count,
            RedriveExceptions = unsuccessfulMessages,
        };
    }
}
