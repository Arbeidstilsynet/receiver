using Arbeidstilsynet.MeldingerReceiver.API.Ports;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Extensions;

internal static class EditMeldingRequestExtensions
{
    extension(EditMeldingRequest editRequest)
    {
        public IEnumerable<Guid> AllDocumentIds()
        {
            if (editRequest.MainContentId != null && editRequest.MainContentId != Guid.Empty)
            {
                yield return editRequest.MainContentId.Value;
            }
            
            if (editRequest.StructuredDataId != null && editRequest.StructuredDataId != Guid.Empty)
            {
                yield return editRequest.StructuredDataId.Value;
            }

            if (editRequest.AttachmentReferenceIds != null)
            {
                foreach (var attachmentId in editRequest.AttachmentReferenceIds.Where(attachmentId => attachmentId != Guid.Empty))
                {
                    yield return attachmentId;
                }
            }
        }
        
    }
}