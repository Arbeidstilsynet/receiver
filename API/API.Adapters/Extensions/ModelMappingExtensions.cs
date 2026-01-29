using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;

public static class ModelMappingExtensions
{
    public static UploadDocumentRequest ToUploadDocumentRequest(this IFormFile file)
    {
        return new UploadDocumentRequest()
        {
            FileMetadata = new FileMetadata
            {
                ContentType = file.ContentType,
                FileName = file.FileName,
            },
            InputStream = file.OpenReadStream(),
        };
    }
}
