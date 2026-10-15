using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IDocumentRepository
{
    Task<Document?> GetDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken,
        bool includeDeleted = false
    );

    Task<List<Document>> GetAllDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken,
        bool includeDeleted = false
    );
}
