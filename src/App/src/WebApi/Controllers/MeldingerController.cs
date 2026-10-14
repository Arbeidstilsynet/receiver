using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Arbeidstilsynet.MeldingerReceiver.App.Extensions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public partial class MeldingerController : ControllerBase
{
    private readonly ApiMeters _apiMeters;
    private readonly IValidator<PostMeldingBody> _postMeldingValidator;
    private readonly IValidator<AttachmentDetachmentRequest> _attachmentDetachmentValidator;
    private readonly IMeldingService _meldingService;
    private readonly ILogger<MeldingerController> _logger;

    public MeldingerController(
        IValidator<PostMeldingBody> postMeldingValidator,
        IValidator<AttachmentDetachmentRequest> attachmentDetachmentValidator,
        IMeldingService meldingService,
        ApiMeters apiMeters,
        ILogger<MeldingerController> logger
    )
    {
        _postMeldingValidator = postMeldingValidator;
        _attachmentDetachmentValidator = attachmentDetachmentValidator;
        _meldingService = meldingService;
        _apiMeters = apiMeters;
        _logger = logger;
    }

    [HttpPost]
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
            MeldingId = model.MeldingId ?? Guid.NewGuid(),
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

    [HttpGet]
    public async Task<ActionResult<GetAllMeldingerResponse>> GetMeldinger(
        [FromQuery] [Range(1, int.MaxValue)] int? pageNumber,
        [FromQuery] [Range(10, 100)] int? pageSize
    )
    {
        var response = await _meldingService.GetMeldinger(pageNumber, pageSize);
        return new GetAllMeldingerResponse
        {
            Items = response.Items,
            PageNumber = response.PageNumber,
            PageSize = response.PageSize,
            TotalPages = response.TotalPages,
            TotalRecords = response.TotalRecords,
        };
    }

    [HttpGet("{meldingId:guid}")]
    public async Task<ActionResult<GetMeldingResponse>> GetMelding(
        [Required] [FromRoute] Guid meldingId,
        CancellationToken cancellationToken
    )
    {
        var melding = await _meldingService.GetMelding(
            new GetMeldingRequest { MeldingId = meldingId },
            cancellationToken
        );

        if (melding == null)
            return NotFound();

        return new GetMeldingResponse { Melding = melding };
    }

    /// <summary>
    /// Detaches one explicitly selected attachment from a supported Altinn melding.
    /// </summary>
    /// <remarks>
    /// The default request is a dry-run. Set <c>apply</c> to true and pass the revision returned
    /// by a current preview to persist the detachment.
    /// </remarks>
    [Authorize]
    [HttpPost("{meldingId:guid}/attachment-detachments")]
    public async Task<ActionResult<AttachmentDetachmentResponse>> DetachAttachment(
        [Required] [FromRoute] Guid meldingId,
        [FromBody] AttachmentDetachmentRequest request,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await _attachmentDetachmentValidator.ValidateAsync(
            request,
            cancellationToken
        );
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToString());
        }

        var response = await _meldingService.DetachAttachment(
            meldingId,
            request,
            cancellationToken
        );
        var actor =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("azp")?.Value
            ?? User.Identity?.Name
            ?? "authenticated";
        _logger.LogInformation(
            "Attachment detachment completed for {MeldingId} and {DocumentId}: applied {Applied}, already detached {AlreadyDetached}; attachments before {AttachmentIdsBefore}, after {AttachmentIdsAfter}; actor {Actor}",
            response.MeldingId,
            response.DocumentId,
            response.Applied,
            response.AlreadyDetached,
            response.AttachmentIdsBefore,
            response.AttachmentIdsAfter,
            actor
        );

        return Ok(response);
    }

    /// <summary>
    /// Resolves a melding by its short id, i.e. the trailing 12 hex characters of the melding GUID
    /// (the last GUID segment, as shown to users in the public frontend).
    /// </summary>
    /// <remarks>
    /// A short id is not guaranteed to be unique. When several meldinger share the same short id,
    /// the endpoint returns <c>409 Conflict</c> together with the list of matching full ids so the
    /// caller can disambiguate using <see cref="GetMelding"/>.
    /// </remarks>
    [HttpGet("by-short-id/{shortId}")]
    [ProducesResponseType<GetMeldingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<GetMeldingByShortIdConflictResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GetMeldingResponse>> GetMeldingByShortId(
        [Required] [FromRoute] string shortId,
        CancellationToken cancellationToken
    )
    {
        if (!ShortIdRegex().IsMatch(shortId))
        {
            return BadRequest(
                "shortId must be the trailing 12 hex characters of the melding GUID."
            );
        }

        var matches = await _meldingService.GetMeldingerByShortId(shortId, cancellationToken);

        return matches.Count switch
        {
            0 => NotFound(),
            1 => new GetMeldingResponse { Melding = matches[0] },
            _ => Conflict(
                new GetMeldingByShortIdConflictResponse
                {
                    MatchingMeldingIds = matches.Select(m => m.Id).ToList(),
                }
            ),
        };
    }

    [GeneratedRegex("^[0-9a-fA-F]{12}$")]
    private static partial Regex ShortIdRegex();
}
