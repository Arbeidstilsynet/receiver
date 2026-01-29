using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Jobs;
using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConsumerManifest>> PostSubscription(
        [FromBody] ConsumerManifest consumerManifest
    )
    {
        return await subscriptionService.CreateSubscription(consumerManifest);
    }

    [HttpGet]
    public async Task<ActionResult<List<ConsumerManifest>>> GetSubscriptions()
    {
        return (await subscriptionService.GetAllSubscriptions()).ToList();
    }
}
