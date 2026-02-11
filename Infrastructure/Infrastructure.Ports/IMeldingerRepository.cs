using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IMeldingRepository
{
    Task<Melding?> UpdateMelding(Melding melding);
    Task<Melding> CreateMelding(CreateMeldingRequest request);
    Task<Melding?> GetMeldingAsync(Guid meldingId);

    Task<PaginationResponse<Melding>> GetMeldingerAsync(int pageSize, int pageNumber = 1);
}
