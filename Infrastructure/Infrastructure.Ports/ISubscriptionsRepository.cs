using Arbeidstilsynet.Common.Altinn.Model.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface ISubscriptionsRepository
{
    public Task<IEnumerable<AltinnConnection>> CreateSubscription(
        ConsumerManifest consumerManifest
    );

    public Task<ConsumerManifest?> GetPersistedSubscription(string consumerName);

    public Task<IList<ConsumerManifest>> GetSubscriptions();

    public Task<AppRegistration?> GetActiveAppRegistration(
        MessageSource messageSource,
        string appId
    );

    public Task DeleteSubscription(ConsumerManifest consumerManifest);

    public Task<IEnumerable<AltinnConnection>> GetAllActiveAltinnSubscriptions();

    public Task<AltinnConnection?> GetActiveAltinnSubscription(string altinnAppId);

    public Task UpdateAltinnSubscriptionId(Guid altinnSubscriptionEntity, int subscriptionId);
}
