using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IAltinnRegistrationService
{
    public Task<AltinnEventsSubscription> RegisterAltinnApplication(string appId);

    public Task<bool> UnsubscribeAltinnApplication(int altinnSubscriptionId);

    public Task<AltinnEventsSubscription?> GetAltinnRegistrationById(int altinnSubscriptionId);
}
