using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;

public interface IPostMeldingPersistedAction
{
    public string Name { get; }
    public Task RunPostActionFor(Melding melding);
}
