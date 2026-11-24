using System.ComponentModel.DataAnnotations;
using System.Net;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Http;
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

    /// <summary>
    /// Soft-deletes an attachment. The row and stored file are kept, but the document is hidden
    /// from all reads until it is restored. Main content and structured data cannot be deleted.
    /// </summary>
    [HttpDelete("{meldingId:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeleteDocument(
        [Required] [FromRoute] Guid meldingId,
        [Required] [FromRoute] Guid documentId,
        CancellationToken cancellationToken
    ) => SetDocumentDeleted(meldingId, documentId, deleted: true, cancellationToken);

    /// <summary>
    /// Restores a soft-deleted attachment.
    /// </summary>
    [HttpPost("{meldingId:guid}/documents/{documentId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RestoreDocument(
        [Required] [FromRoute] Guid meldingId,
        [Required] [FromRoute] Guid documentId,
        CancellationToken cancellationToken
    ) => SetDocumentDeleted(meldingId, documentId, deleted: false, cancellationToken);

    private async Task<IActionResult> SetDocumentDeleted(
        Guid meldingId,
        Guid documentId,
        bool deleted,
        CancellationToken cancellationToken
    )
    {
        var found = await _documentService.SetDocumentDeleted(
            meldingId,
            documentId,
            deleted,
            cancellationToken
        );
        return found ? NoContent() : NotFound();
    }

    /// <summary>
    /// Downloads an active document. Restore a soft-deleted attachment before downloading it.
    /// </summary>
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
        var documents = await _documentService.GetAllDocuments(
            new GetAllDocumentsRequest { MeldingId = meldingId },
            cancellationToken
        );

        if (documents == null)
            return NotFound();

        return new GetAllDocumentsResponse
        {
            Documents = documents.OrderBy(d => d.FileMetadata.FileName).ToList(),
        };
    }

    /// <summary>
    /// Lists soft-deleted attachments on a melding, including when they were deleted.
    /// </summary>
    [HttpGet("{meldingId:guid}/documents/deleted")]
    public async Task<ActionResult<GetAllDocumentsResponse>> GetDeletedDocuments(
        [Required] [FromRoute] Guid meldingId,
        CancellationToken cancellationToken
    )
    {
        var documents = await _documentService.GetDeletedDocuments(
            new GetAllDocumentsRequest { MeldingId = meldingId },
            cancellationToken
        );

        if (documents == null)
            return NotFound();

        return new GetAllDocumentsResponse
        {
            Documents = documents.OrderBy(d => d.FileMetadata.FileName).ToList(),
        };
    }
}
