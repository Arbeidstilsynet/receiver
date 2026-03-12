using System.ComponentModel.DataAnnotations;
using System.Net;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class MeldingerController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IMeldingService _meldingService;

    public MeldingerController(IMeldingService meldingService, IDocumentService documentService)
    {
        _meldingService = meldingService;
        _documentService = documentService;
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

    [HttpGet("{meldingId:guid}/documents/{documentId:guid}")]
    public async Task<ActionResult<Document>> GetDocument(
        [Required] [FromRoute] Guid meldingId,
        [Required] [FromRoute] Guid documentId,
        CancellationToken cancellationToken
    )
    {
        var request = new GetDocumentRequest { MeldingId = meldingId, DocumentId = documentId };

        var document = await _documentService.GetDocument(request, cancellationToken);

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

        var document = await _documentService.GetDocument(request, cancellationToken);

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
        [Required] [FromRoute] Guid meldingId,
        CancellationToken cancellationToken
    )
    {
        var request = new GetAllDocumentsRequest { MeldingId = meldingId };

        var documents = await _documentService.GetAllDocuments(request, cancellationToken);

        if (documents == null)
            return NotFound();

        return new GetAllDocumentsResponse
        {
            Documents = documents.OrderBy(d => d.FileMetadata.FileName).ToList(),
        };
    }
}
