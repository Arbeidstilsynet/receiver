using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IDocumentRepository
{
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<List<Document>> GetAllDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken
    );

    Task<List<Document>> GetDeletedDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Soft-deletes (<paramref name="deleted"/> = true) or restores an attachment on a melding.
    /// Returns false when the document does not exist on the melding.
    /// </summary>
    Task<bool> SetDeleted(
        Guid meldingId,
        Guid documentId,
        bool deleted,
        CancellationToken cancellationToken
    );
}
