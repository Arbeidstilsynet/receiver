using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IMeldingNotificationService
{
    public Task NotifyMeldingProcessed(Melding melding);
}
