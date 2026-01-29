using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Meldinger.Receiver.Model.Response;

public record GetMeldingResponse
{
    public Melding? Melding { get; init; }
}
