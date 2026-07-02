using System.ComponentModel.DataAnnotations;
using System.Net;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

[ApiController]
[Route("meldinger")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
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
