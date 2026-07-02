using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Bogus;
using static Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test.Extensions.FakerExtensions;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test.fixtures;

public static class TestData
{
    public static Faker<Melding> CreateMeldingFaker() => CreateFaker<Melding>();

    public static Faker<Document> DocumentFakerCreate() => CreateFaker<Document>();

    public static Faker<FileMetadata> CreateFileMetadataFaker() =>
        CreateFaker<FileMetadata>()
            .RuleFor(f => f.FileName, f => f.System.FileName())
            .RuleFor(f => f.ContentType, f => f.System.MimeType());
}
