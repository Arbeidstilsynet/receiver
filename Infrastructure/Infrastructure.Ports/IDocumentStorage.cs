using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IDocumentStorage
{
    public Task<UploadResponse> Upload(UploadRequest request, CancellationToken cancellationToken);
    public Task Download(
        string internalDocumentId,
        Stream outputStream,
        CancellationToken cancellationToken
    );

    public Task Download(
        Document document,
        Stream outputStream,
        CancellationToken cancellationToken
    );
}
