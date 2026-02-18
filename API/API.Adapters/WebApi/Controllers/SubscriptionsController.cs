using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
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
