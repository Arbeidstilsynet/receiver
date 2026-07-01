namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;

public record GetAllDocumentsRequest
{
    public Guid MeldingId { get; init; }
}
