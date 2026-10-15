using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using FluentValidation;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Validation;

internal class AttachmentDetachmentRequestValidator : AbstractValidator<AttachmentDetachmentRequest>
{
    public AttachmentDetachmentRequestValidator()
    {
        RuleFor(request => request.DocumentId).NotEmpty();
        When(
            request => request.Apply,
            () => RuleFor(request => request.ExpectedRevision).NotEmpty()
        );
    }
}
