namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

public class DocumentDeletionNotAllowedException(Guid documentId)
    : Exception($"Document '{documentId}' is not an attachment and cannot be deleted or restored.");
