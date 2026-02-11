using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

internal class AdHocController : ControllerBase
{
    private readonly IMeldingRepository _meldingRepository;

    public AdHocController(IMeldingRepository meldingRepository)
    {
        _meldingRepository = meldingRepository;
    }
    
    [HttpPost("/editMelding")]
    public async Task<ActionResult<Melding>> PostMeldingEdit(
        [FromBody] PostEditMeldingBody model,
        CancellationToken cancellationToken
    )
    {
        var melding = await _meldingRepository.GetMeldingAsync(model.MeldingId, cancellationToken);
        if (melding == null)
        {
            return NotFound();
        }
        
        
        
        
    }
}