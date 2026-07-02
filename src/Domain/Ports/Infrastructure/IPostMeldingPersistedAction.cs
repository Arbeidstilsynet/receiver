using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;

public interface IPostMeldingPersistedAction
{
    public string Name { get; }
    public Task RunPostActionFor(Melding melding);
}
