using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IDocumentRepository
{
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<List<Document>> GetAllDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken
    );
}
