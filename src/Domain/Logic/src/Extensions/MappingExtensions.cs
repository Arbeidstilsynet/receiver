using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Extensions;

internal static class MappingExtensions
{
    public static UploadRequest ToUploadRequest(this UploadDocumentRequest request, Guid meldingId)
    {
        return new UploadRequest
        {
            Document = request.CreateDocument(meldingId),
            InputStream = request.InputStream,
        };
    }

    private static Document CreateDocument(this UploadDocumentRequest request, Guid meldingId)
    {
        return new Document
        {
            DocumentId = request.DocumentId ?? Guid.NewGuid(),
            MeldingId = meldingId,
            FileMetadata = request.FileMetadata,
            ScanResult = request.ScanResult,
            Tags = request.Tags,
        };
    }
}
