using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.AdHoc;

public interface IAdHocMigrateMainDocument
{
    public Task<Melding?> MigrateMainDocument(
        Guid meldingId,
        Guid mainDocument,
        Guid structuredData,
        IEnumerable<Guid> attachments,
        CancellationToken cancellationToken
    );
}
