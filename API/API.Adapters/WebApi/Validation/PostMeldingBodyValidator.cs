using System.Data;
using Arbeidstilsynet.Receiver.Model.Request;
using FluentValidation;
using FluentValidation.Results;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Validation;

internal class PostMeldingBodyValidator : AbstractValidator<PostMeldingBody>
{
    public PostMeldingBodyValidator()
    {
        RuleFor(x => x.StructuredData)
            .Must(x => x?.ContentType == "application/json")
            .WithMessage(
                $"{nameof(PostMeldingBody.StructuredData)} must have content type application/json"
            );

        RuleFor(x => x.MainContent)
            .Must(x => x?.ContentType != "application/json")
            .WithMessage(
                $"{nameof(PostMeldingBody.MainContent)} must NOT have content type application/json"
            );
    }
}
