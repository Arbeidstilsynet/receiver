namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public record GetAllDocumentsRequest
{
    public Guid MeldingId { get; init; }
}
