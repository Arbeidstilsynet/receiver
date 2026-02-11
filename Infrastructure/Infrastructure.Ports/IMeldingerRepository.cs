using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IMeldingRepository
{
    Task<Melding> CreateMelding(CreateMeldingRequest request, CancellationToken cancellationToken);
    Task<Melding?> GetMeldingAsync(Guid meldingId, CancellationToken cancellationToken);

    Task<PaginationResponse<Melding>> GetMeldingerAsync(int pageSize, int pageNumber = 1);
}
