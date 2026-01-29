using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IAltinnRegistrationService
{
    public Task<AltinnEventsSubscription> RegisterAltinnApplication(string appId);

    public Task<bool> UnsubscribeAltinnApplication(int altinnSubscriptionId);

    public Task<AltinnEventsSubscription?> GetAltinnRegistrationById(int altinnSubscriptionId);
}
