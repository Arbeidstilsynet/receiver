using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Storage;

internal static class UploadRequestExtensions
{
    public static string CreateGcsObjectName(this UploadRequest uploadRequest)
    {
        return $"{uploadRequest.Document.MeldingId}/{uploadRequest.Document.DocumentId}";
    }
}
