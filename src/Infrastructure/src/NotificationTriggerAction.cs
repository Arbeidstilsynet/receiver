using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure;

internal class NotificationTriggerAction(
    IMeldingNotificationService meldingNotificationService,
    ILogger<NotificationTriggerAction> logger
) : IPostMeldingPersistedAction
{
    public string Name => nameof(NotificationTriggerAction);

    public async Task RunPostActionFor(Melding melding)
    {
        using var activity = Tracer.Source.StartActivity("run NotificationTriggerAction");
        logger.LogInformation("Trigger melding notification...");
        await meldingNotificationService.NotifyMeldingProcessed(melding);
    }
}
