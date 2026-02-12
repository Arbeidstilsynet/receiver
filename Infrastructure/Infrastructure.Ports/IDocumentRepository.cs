using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IDocumentRepository
{
    Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<List<Document>> GetAllDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken
    );
}
