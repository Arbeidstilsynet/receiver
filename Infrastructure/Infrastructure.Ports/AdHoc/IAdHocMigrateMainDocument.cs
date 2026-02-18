using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.AdHoc;

public interface IAdHocMigrateMainDocument
{
    public Task<Melding?> MigrateMainDocument(
        Guid meldingId,
        Guid newMainContent,
        CancellationToken cancellationToken
    );
}
