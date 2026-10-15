namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

public class MeldingNotFoundException(Guid meldingId)
    : Exception($"Melding '{meldingId}' was not found.");
