namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;

public record GetMeldingRequest
{
    public Guid MeldingId { get; init; }
    public string Mottaker { get; init; }
}
