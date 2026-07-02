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

    /// <summary>
    /// Resolves meldinger by their short id (the trailing 12 hex characters of the melding GUID).
    /// A short id is not guaranteed to be unique, so the result may contain more than one melding.
    /// </summary>
    public Task<IReadOnlyList<Melding>> GetMeldingerByShortId(
        string shortId,
        CancellationToken cancellationToken
    );

    Task<Domain.Ports.App.PaginationResponse<Melding>> GetMeldinger(
        int? pageNumber = 1,
        int? pageSize = 10
    );
}
