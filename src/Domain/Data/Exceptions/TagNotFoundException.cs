namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

public class TagNotFoundException : Exception
{
    public TagNotFoundException(string tagName, Guid meldingId)
        : base($"Tag with id '{tagName}' was not found in melding with MeldingId {meldingId}") { }
}
