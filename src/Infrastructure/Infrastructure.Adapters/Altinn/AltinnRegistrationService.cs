using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Altinn;

internal class AltinnRegistrationService(
    IAltinnAdapter altinnAdapter,
    IOptions<InfrastructureConfiguration> options,
    ILogger<AltinnRegistrationService> logger,
    IMapper mapper
) : IAltinnRegistrationService
{
    public async Task<bool> UnsubscribeAltinnApplication(int altinnSubscriptionId)
    {
        var subscription = await altinnAdapter.GetAltinnSubscription(altinnSubscriptionId);
        if (subscription == null)
        {
            logger.LogWarning(
                "No entity found for the given id: {AltinnSubscriptionId}. Could not unsubscribe from altinn.",
                altinnSubscriptionId
            );
        }
        return await altinnAdapter.UnsubscribeForCompletedProcessEvents(
            subscription
                ?? new Common.Altinn.Model.Api.Response.AltinnSubscription
                {
                    Id = altinnSubscriptionId,
                }
        );
    }

    public async Task<AltinnEventsSubscription?> GetAltinnRegistrationById(int altinnSubscriptionId)
    {
        var subscription = await altinnAdapter.GetAltinnSubscription(altinnSubscriptionId);
        if (subscription == null)
        {
            return null;
        }
        return mapper.Map<AltinnEventsSubscription>(subscription);
    }

    public async Task<AltinnEventsSubscription> RegisterAltinnApplication(string appId)
    {
        var subscription = await altinnAdapter.SubscribeForCompletedProcessEvents(
            new Common.Altinn.Model.Adapter.SubscriptionRequestDto
            {
                AltinnAppId = appId,
                CallbackUrl = new Uri(
                    new Uri(options.Value.AppDomain),
                    "webhook/receive-altinn-cloudevent"
                ),
            }
        );
        return mapper.Map<AltinnEventsSubscription>(subscription);
    }
}
