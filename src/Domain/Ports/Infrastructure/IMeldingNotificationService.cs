using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IMeldingNotificationService
{
    public Task NotifyMeldingProcessed(Melding melding);
}
