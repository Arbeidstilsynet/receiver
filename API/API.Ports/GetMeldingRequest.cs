namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public record GetMeldingRequest
{
    public Guid MeldingId { get; init; }
    public string Mottaker { get; init; }
}
