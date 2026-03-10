using System.Net.Mime;
using System.Text;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.Receiver.Model.Request;
using Bogus;
using Microsoft.AspNetCore.Http;
using static Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test.Extensions.FakerExtensions;
using FileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test.fixture;

public static class TestData
{
    public static Faker<PostMeldingBody> CreatePostMeldingBodyFaker() =>
        CreateFaker<PostMeldingBody>()
            .UseSeed(1337)
            .RuleFor(
                f => f.ApplicationId,
                f => f.PickRandom(ApplicationFixture.KnownApplicationIds)
            )
            .RuleFor(f => f.Metadata, f => f.CreateDictionary());

    public static Faker<UploadDocumentRequest> CreateUploadDocumentRequestFaker() =>
        CreateFaker<UploadDocumentRequest>()
            .UseSeed(1337)
            .RuleFor(
                f => f.FileMetadata,
                f => Domain.Logic.Test.fixtures.TestData.CreateFileMetadataFaker().Generate()
            )
            .RuleFor(f => f.ScanResult, f => DocumentScanResult.Clean)
            .RuleFor(f => f.InputStream, f => f.CreateStream(f.Random.Int(100, 1000)));

    public static Faker<CreateMeldingRequest> CreatePostMeldingRequestFaker() =>
        CreateFaker<CreateMeldingRequest>()
            .UseSeed(1337)
            .RuleFor(r => r.Source, f => f.PickRandom<MessageSource>())
            .RuleFor(
                r => r.Attachments,
                f => f.Make(f.Random.Int(1, 5), () => CreateUploadDocumentRequestFaker().Generate())
            )
            .RuleFor(r => r.Metadata, f => f.CreateDictionary())
            .RuleFor(r => r.MainContent, f => CreateUploadDocumentRequestFaker().Generate())
            .RuleFor(r => r.StructuredData, f => CreateUploadDocumentRequestFaker().Generate());

    public static Faker<AltinnSubscription> CreateSubscriptionFaker() =>
        CreateFaker<AltinnSubscription>().UseSeed(1337);

    public static Faker<FileMetadata> CreateAltinnFileMetadataFaker() =>
        CreateFaker<FileMetadata>()
            .UseSeed(1337)
            .RuleFor(m => m.Filename, f => f.System.FileName())
            .RuleFor(m => m.ContentType, f => f.System.MimeType());

    public static Faker<AltinnDocument> CreateAltinnDocumentFaker() =>
        CreateFaker<AltinnDocument>()
            .UseSeed(1337)
            .RuleFor(d => d.FileMetadata, _ => CreateAltinnFileMetadataFaker().Generate())
            .RuleFor(d => d.DocumentContent, f => f.CreateStream(f.Random.Int(100, 1000)));

    public static Faker<AltinnInstanceSummary> CreateAltinnInstanceSummaryFaker() =>
        CreateFaker<AltinnInstanceSummary>()
            .UseSeed(1337)
            .RuleFor(s => s.Metadata, f => CreateFaker<AltinnMetadata>().Generate())
            .RuleFor(s => s.SkjemaAsPdf, f => CreateAltinnDocumentFaker().Generate())
            .RuleFor(s => s.StructuredData, f => CreateAltinnDocumentFaker().Generate())
            .RuleFor(
                s => s.Attachments,
                f => f.Make(f.Random.Int(1, 5), () => CreateAltinnDocumentFaker().Generate())
            );

    public static AltinnCloudEvent CloudEvent(Action<AltinnCloudEvent>? customize = null)
    {
        var faker = CreateFaker<AltinnCloudEvent>()
            .RuleFor(
                e => e.DataContentType,
                new ContentType(MediaTypeNames.Application.Json)
                {
                    Name = MediaTypeNames.Application.Json,
                }
            );

        var cloudEvent = faker.Generate();

        customize?.Invoke(cloudEvent);

        return cloudEvent;
    }

    public static IFormFile CreateFormFile(
        string name,
        string content,
        string contentType = "text/plain"
    )
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, content.Length, name, name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
