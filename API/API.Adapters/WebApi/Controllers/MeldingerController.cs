using System.ComponentModel.DataAnnotations;
using System.Net;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class MeldingerController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ApiMeters _apiMeters;
    private readonly IMeldingService _meldingService;

    public MeldingerController(
        IMeldingService meldingService,
        IDocumentService documentService,
        ApiMeters apiMeters
    )
    {
        _meldingService = meldingService;
        _documentService = documentService;
        _apiMeters = apiMeters;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PostMeldingResponse>> PostMelding(
        [FromForm] PostMeldingBody model,
        CancellationToken cancellationToken
    )
    {
        var meldingReceivedAt = DateTime.Now;
        _apiMeters.MeldingReceived(MessageSource.Api, model.ApplicationId);
        var postMeldingRequest = new PostMeldingRequest
        {
            MeldingId = Guid.NewGuid(),
            Source = MessageSource.Api,
            ApplicationReference = model.ApplicationId,
            MeldingReceivedAt = meldingReceivedAt,
            MainContent = model.MainContent.ToUploadDocumentRequest(),
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
        [Required] [FromRoute] Guid meldingId
    )
    {
        var melding = await _meldingService.GetMelding(
            new GetMeldingRequest { MeldingId = meldingId }
        );

        if (melding == null)
            return NotFound();

        return new GetMeldingResponse { Melding = melding };
    }

    [HttpGet("{meldingId:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<Document>> GetDocument(
        [Required] [FromRoute] Guid meldingId,
        [Required] [FromRoute] Guid documentId
    )
    {
        var request = new GetDocumentRequest { MeldingId = meldingId, DocumentId = documentId };

        var document = await _documentService.GetDocument(request);

        if (document == null)
            return NotFound();
        return document;
    }

    [HttpGet("{meldingId:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(
        [Required] [FromRoute] Guid meldingId,
        [Required] [FromRoute] Guid documentId,
        CancellationToken cancellationToken
    )
    {
        var request = new GetDocumentRequest { MeldingId = meldingId, DocumentId = documentId };

        var document = await _documentService.GetDocument(request);

        if (document == null)
            return NotFound();

        Response.ContentType = document.FileMetadata.ContentType;
        var urlEncodedFileName = WebUtility.UrlEncode(document.FileMetadata.FileName);
        Response.Headers.Append(
            "Content-Disposition",
            $"attachment; filename=\"{urlEncodedFileName}\""
        );

        await _documentService.DownloadDocument(document, Response.Body, cancellationToken);

        return Empty;
    }

    [HttpGet("{meldingId:guid}/documents")]
    public async Task<ActionResult<GetAllDocumentsResponse>> GetAllDocuments(
        [Required] [FromRoute] Guid meldingId
    )
    {
        var request = new GetAllDocumentsRequest { MeldingId = meldingId };

        var documents = await _documentService.GetAllDocuments(request);

        if (documents == null)
            return NotFound();

        return new GetAllDocumentsResponse
        {
            Documents = documents.OrderBy(d => d.FileMetadata.FileName).ToList(),
        };
    }
}
