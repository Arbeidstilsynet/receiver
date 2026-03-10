using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.App.Extensions;

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

    public static UploadDocumentRequest AsCleanFile(this UploadDocumentRequest request)
    {
        return new UploadDocumentRequest()
        {
            FileMetadata = request.FileMetadata,
            InputStream = request.InputStream,
        };
    }
}
