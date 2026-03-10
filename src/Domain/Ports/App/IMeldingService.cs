using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;

public interface IMeldingService
{
    public Task<Melding> ProcessMelding(
        CreateMeldingRequest request,
        CancellationToken cancellationToken
    );
    public Task<Melding?> GetMelding(
        GetMeldingRequest request,
        CancellationToken cancellationToken
    );

    Task<Domain.Ports.App.PaginationResponse<Melding>> GetMeldinger(
        int? pageNumber = 1,
        int? pageSize = 10
    );
}
