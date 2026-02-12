using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.AdHoc;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AdHocController(IAdHocMigrateMainDocument adHocMigrate, IMeldingNotificationService meldingNotificationService) : ControllerBase
{
    [HttpPost("editMelding")]
    public async Task<ActionResult<Melding>> PostMeldingEdit(
        [FromBody] PostEditMeldingBody model,
        CancellationToken cancellationToken
    )
    {
        var result = await adHocMigrate.MigrateMainDocument(
            model.MeldingId,
            model.NewMainContentId,
            cancellationToken
        );

        if (result == null)
        {
            return NotFound();
        }

        await meldingNotificationService.NotifyMeldingProcessed(result);

        return Ok(result);
    }
}
