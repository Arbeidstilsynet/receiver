namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

public class AttachmentDetachmentConflictException()
    : Exception("The attachment detachment revision is stale.") { }
