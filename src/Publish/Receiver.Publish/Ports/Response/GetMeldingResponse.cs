using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Model.Response;

public record GetMeldingResponse
{
    public Melding? Melding { get; init; }
}
