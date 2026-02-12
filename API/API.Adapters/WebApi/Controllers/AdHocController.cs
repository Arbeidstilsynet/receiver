using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.AdHoc;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AdHocController : ControllerBase
{
    private readonly IAdHocMigrateMainDocument _adHocMigrate;

    public AdHocController(IAdHocMigrateMainDocument adHocMigrate)
    {
        _adHocMigrate = adHocMigrate;
    }
    
    [HttpPost("/editMelding")]
    public async Task<ActionResult<Melding>> PostMeldingEdit(
        [FromBody] PostEditMeldingBody model,
        CancellationToken cancellationToken
    )
    {
        var result = await _adHocMigrate.MigrateMainDocument(model.MeldingId, model.MainContentId, model.StructuredDataId, model.AttachmentReferenceIds, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }
        
        return Ok(result);
    }
}