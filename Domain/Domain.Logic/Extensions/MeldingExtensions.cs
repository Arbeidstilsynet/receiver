using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Extensions;

internal static class MeldingExtensions
{
    public static bool ContainsDocument(this Melding? melding, Guid documentId)
    {
        return
            melding != null &&
            (melding.MainContentId == documentId
             || melding.StructuredDataId == documentId
             || melding.AttachmentIds.Contains(documentId));
    }
    
}