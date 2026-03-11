using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Storage;

internal static class UploadRequestExtensions
{
    public static string CreateGcsObjectName(this UploadRequest uploadRequest)
    {
        return $"{uploadRequest.Document.MeldingId}/{uploadRequest.Document.DocumentId}";
    }
}
