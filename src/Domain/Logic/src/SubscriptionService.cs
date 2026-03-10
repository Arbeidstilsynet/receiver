using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal class SubscriptionService(
    IAltinnRegistrationService altinnService,
    ISubscriptionsRepository subscriptionsRepository,
    IOptions<DomainConfiguration> domainConfiguration
) : ISubscriptionService
{
    public async Task<ConsumerManifest> CreateSubscription(ConsumerManifest request)
    {
        // check first if we already have a subscription for consumer
        // subscription repo GET

        var existingManifest = await subscriptionsRepository.GetPersistedSubscription(
            request.ConsumerName
        );

        if (existingManifest != null)
        {
            if (!existingManifest.Diff(request))
            {
                return existingManifest;
            }

            // Delete old
            foreach (
                var altinnApp in existingManifest.AppRegistrations.Where(s =>
                    s.MessageSource == MessageSource.Altinn
                )
            )
            {
                var altinnConnection = await subscriptionsRepository.GetActiveAltinnSubscription(
                    altinnApp.AppId
                );
                if (altinnConnection?.SubscriptionId != null)
                {
                    var successful = await altinnService.UnsubscribeAltinnApplication(
                        (int)altinnConnection.SubscriptionId
                    );
                    if (!successful && domainConfiguration.Value.RequireAltinnDeletionOnUnsubscribe)
                    {
                        throw new ArgumentException(
                            $"No entity found for the given id: {(int)altinnConnection.SubscriptionId}. Could not unsubscribe from altinn."
                        );
                    }
                }
            }
            await subscriptionsRepository.DeleteSubscription(existingManifest);
        }

        // Write to db
        var altinnAppsToUpdate = await subscriptionsRepository.CreateSubscription(request);

        // if not, start registering altinn apps first
        foreach (var altinnAppRegistration in altinnAppsToUpdate)
        {
            var altinnSubscription = await altinnService.RegisterAltinnApplication(
                altinnAppRegistration.AltinnAppId
            );
            await subscriptionsRepository.UpdateAltinnSubscriptionId(
                altinnAppRegistration.InternalId,
                altinnSubscription.Id
            );
        }
        return request;
    }

    public async Task<int?> GetActiveAltinnSubscriptionId(string altinnAppId)
    {
        var altinnConnection = await subscriptionsRepository.GetActiveAltinnSubscription(
            altinnAppId
        );
        return altinnConnection?.SubscriptionId != null ? altinnConnection.SubscriptionId : null;
    }

    public Task<IList<ConsumerManifest>> GetAllSubscriptions()
    {
        return subscriptionsRepository.GetSubscriptions();
    }

    public async Task<bool> ShouldMeldingForAppIdBeIgnored(
        MessageSource messageSource,
        string appId
    )
    {
        return domainConfiguration.Value.AllowOnlyRegisteredApps
            && (await subscriptionsRepository.GetActiveAppRegistration(messageSource, appId))
                == null;
    }
}

file static class Extensions
{
    public static bool Diff(this ConsumerManifest existing, ConsumerManifest incoming)
    {
        return !existing.Equals(incoming);
    }
}
