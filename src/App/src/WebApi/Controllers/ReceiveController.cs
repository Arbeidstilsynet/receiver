using Arbeidstilsynet.MeldingerReceiver.App.Extensions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ReceiveController : ControllerBase
{
    private readonly ApiMeters _apiMeters;
    private readonly IValidator<PostMeldingBody> _postMeldingValidator;
    private readonly IMeldingService _meldingService;

    public ReceiveController(
        IValidator<PostMeldingBody> postMeldingValidator,
        IMeldingService meldingService,
        ApiMeters apiMeters
    )
    {
        _postMeldingValidator = postMeldingValidator;
        _meldingService = meldingService;
        _apiMeters = apiMeters;
    }

    [HttpPost("melding")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PostMeldingResponse>> PostMelding(
        [FromForm] PostMeldingBody model,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await _postMeldingValidator.ValidateAsync(model, cancellationToken);

        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToString());
        }

        _apiMeters.MeldingReceived(MessageSource.Api, model.ApplicationId);
        var postMeldingRequest = new CreateMeldingRequest
        {
            MeldingId = Guid.NewGuid(),
            Source = MessageSource.Api,
            ApplicationReference = model.ApplicationId,
            MainContent = model.MainContent?.ToUploadDocumentRequest(),
            StructuredData = model.StructuredData?.ToUploadDocumentRequest(),
            Attachments = model.Attachments.Select(a => a.ToUploadDocumentRequest()).ToList(),
            Metadata = model.Metadata,
        };

        var melding = await _meldingService.ProcessMelding(postMeldingRequest, cancellationToken);
        _apiMeters.MeldingProcessed(melding);
        _apiMeters.RegisterMeldingDuration(melding);
        return new PostMeldingResponse { MeldingId = melding.Id };
    }
}
