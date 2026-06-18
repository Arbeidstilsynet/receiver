using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.App.Extensions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

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
        var postMeldingRequest = await GetAltinnInstanceData(cloudEvent);
        apiMeters.MeldingReceived(
            postMeldingRequest.Source,
            postMeldingRequest.ApplicationReference
        );
        var melding = await meldingService.ProcessMelding(postMeldingRequest, cancellationToken);
        apiMeters.MeldingProcessed(melding);
        apiMeters.RegisterMeldingDuration(melding);
        return Ok(new PostMeldingResponse { MeldingId = melding.Id });
    }

    private async Task<CreateMeldingRequest> GetAltinnInstanceData(AltinnCloudEvent cloudEvent)
    {
        using var activity = Tracer.Source.StartActivity();
        var altinnSummary = await altinnAdapter.GetSummary(cloudEvent);
        return altinnSummary.MapAltinnSummaryToPostMeldingRequest();
    }
}
