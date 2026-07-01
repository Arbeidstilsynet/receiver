namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

public class DocumentNotSafeToUseException : Exception
{
    public DocumentNotSafeToUseException(Document document)
        : base(
            $"Document with id {document.DocumentId} is not safe to use. MeldingId: {document.MeldingId}"
        ) { }

    public DocumentNotSafeToUseException(Document document, Exception innerException)
        : base(
            $"Document with id {document.DocumentId} is not safe to use. MeldingId: {document.MeldingId}",
            innerException
        ) { }
}
