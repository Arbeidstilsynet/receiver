using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AdHocController : ControllerBase
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

        var hangingDocRefs = model.DocumentIds().Except(melding.AllDocumentIds()).ToList();
        
        if (hangingDocRefs.Count != 0)
        {
            return BadRequest(
                $"The following document references do not exist on the melding and cannot be added: {string.Join(", ", hangingDocRefs)}"
            );
        }

        var editedMelding = melding with
        {
            MainContentId = model.MainContentId ?? melding.MainContentId,
            StructuredDataId = model.StructuredDataId ?? melding.StructuredDataId,
            AttachmentIds = model.AttachmentReferenceIds ?? melding.AttachmentIds,
        };
        
        // TODO: Update the melding (document ids) and each document (document type)
        TODO

        return Ok(editedMelding);
    }
}

file static class Extensions
{
    public static IEnumerable<Guid> DocumentIds(this PostEditMeldingBody editRequest)
    {
        var documentIds = new List<Guid>();
        if (editRequest.MainContentId != null)
        {
            documentIds.Add(editRequest.MainContentId.Value);
        }
        if (editRequest.StructuredDataId != null)
        {
            documentIds.Add(editRequest.StructuredDataId.Value);
        }
        if (editRequest.AttachmentReferenceIds != null)
        {
            documentIds.AddRange(editRequest.AttachmentReferenceIds);
        }

        return documentIds;
    }
} 