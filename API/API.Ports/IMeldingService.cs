using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public interface IMeldingService
{
    public Task<Melding?> EditMelding(
        EditMeldingRequest editRequest,
        CancellationToken cancellationToken
    );
    
    public Task<Melding> ProcessMelding(
        CreateMeldingRequest request,
        CancellationToken cancellationToken
    );
    public Task<Melding?> GetMelding(GetMeldingRequest request);

    Task<API.Ports.PaginationResponse<Melding>> GetMeldinger(
        int? pageNumber = 1,
        int? pageSize = 10
    );
}
