using Arbeidstilsynet.Receiver.Model.Request;
using FluentValidation;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Validation;

internal class PostMeldingBodyValidator : AbstractValidator<PostMeldingBody>
{
    public PostMeldingBodyValidator()
    {
        RuleFor(x => x.StructuredData)
            .Must(x => x == null || x.ContentType == "application/json")
            .WithMessage(
                $"{nameof(PostMeldingBody.StructuredData)} must have content type application/json"
            );

        RuleFor(x => x.MainContent)
            .Must(x => x == null || x.ContentType != "application/json")
            .WithMessage(
                $"{nameof(PostMeldingBody.MainContent)} must NOT have content type application/json"
            );

        RuleFor(x => x)
            .Must(x => x.MainContent != null || x.StructuredData != null)
            .WithMessage(
                $"Either {nameof(PostMeldingBody.MainContent)} or {nameof(PostMeldingBody.StructuredData)} must be provided"
            );
    }
}
