using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Notification;

internal class DummyMeldingNotificationService(ILogger<DummyMeldingNotificationService> logger)
    : IMeldingNotificationService
{
    public Task NotifyMeldingProcessed(Melding melding)
    {
        logger.LogWarning(
            "Did not send a notification to event stream because of missing valkey config."
        );
        return Task.CompletedTask;
    }
}
