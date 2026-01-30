using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhookController(
    IMeldingService meldingService,
    IAltinnAdapter altinnAdapter,
    IOptions<InfrastructureConfiguration> options,
    ApiMeters apiMeters
) : ControllerBase
{
    [HttpPost("receive-altinn-cloudevent")]
    public async Task<IActionResult> PostMelding(
        [FromBody] AltinnCloudEvent cloudEvent,
        CancellationToken cancellationToken
    )
    {
        if (cloudEvent.Type == "platform.events.validatesubscription")
        {
            return Ok();
        }
        var meldingReceivedAt = DateTime.Now;
        var postMeldingRequest = await GetAltinnInstanceData(cloudEvent, meldingReceivedAt);
        apiMeters.MeldingReceived(
            postMeldingRequest.Source,
            postMeldingRequest.ApplicationReference
        );
        var melding = await meldingService.ProcessMelding(postMeldingRequest, cancellationToken);
        apiMeters.MeldingProcessed(melding);
        apiMeters.RegisterMeldingDuration(melding);
        return Ok(new PostMeldingResponse { MeldingId = melding.Id });
    }

    private async Task<PostMeldingRequest> GetAltinnInstanceData(
        AltinnCloudEvent cloudEvent,
        DateTime meldingReceivedAt
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var altinnSummary = await altinnAdapter.GetSummary(
            cloudEvent
        );
        return altinnSummary.MapAltinnSummaryToPostMeldingRequest(meldingReceivedAt);
    }
}
