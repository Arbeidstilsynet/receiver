using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IMeldingRepository
{
    Task<Melding> CreateMelding(CreateMeldingRequest request, CancellationToken cancellationToken);
    Task<Melding?> GetMelding(Guid meldingId, CancellationToken cancellationToken);

    Task<PaginationResponse<Melding>> GetMeldinger(int pageSize, int pageNumber = 1);
}
