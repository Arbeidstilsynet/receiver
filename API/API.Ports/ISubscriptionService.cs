using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public interface ISubscriptionService
{
    public Task<ConsumerManifest> CreateSubscription(ConsumerManifest request);

    public Task<IList<ConsumerManifest>> GetAllSubscriptions();

    public Task<int?> GetActiveAltinnSubscriptionId(string altinnAppId);

    public Task<bool> ShouldMeldingForAppIdBeIgnored(MessageSource messageSource, string appId);
}
