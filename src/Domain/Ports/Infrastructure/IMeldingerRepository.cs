using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IMeldingRepository
{
    Task<Melding> CreateMelding(CreateMeldingRequest request, CancellationToken cancellationToken);
    Task<Melding?> GetMelding(Guid meldingId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all meldinger whose GUID ends with the given short id (the trailing 12 hex characters).
    /// A short id is not guaranteed to be unique, so the result may contain more than one melding.
    /// </summary>
    Task<IReadOnlyList<Melding>> GetMeldingerByShortId(
        string shortId,
        CancellationToken cancellationToken
    );

    Task<PaginationResponse<Melding>> GetMeldinger(int pageSize, int pageNumber = 1);
}
