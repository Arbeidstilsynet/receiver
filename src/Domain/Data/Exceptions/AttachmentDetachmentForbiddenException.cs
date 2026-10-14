namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;

public class AttachmentDetachmentForbiddenException()
    : Exception(
        "Attachment detachment is only supported for Altinn messages from ulykkesvarsel and forhandsmelding."
    ) { }
