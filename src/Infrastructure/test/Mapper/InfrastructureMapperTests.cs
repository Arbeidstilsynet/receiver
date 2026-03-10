using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Argon;
using Bogus;
using MapsterMapper;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Mapper;

public class InfrastructureMapperTests : TestBed<InfrastructureAdapterTestFixture>
{
    private static readonly Faker<AltinnSubscriptionEntity> AltinnSubscriptionEntityFaker =
        new Faker<AltinnSubscriptionEntity>()
            .UseSeed(1337)
            .RuleForType(typeof(string), faker => "example-string")
            .RuleForType(typeof(int), faker => faker.Random.Number())
            .RuleForType(typeof(Uri), faker => new Uri(faker.Internet.Url()));

    private readonly IMapper _mapper;
    private readonly VerifySettings _verifySettings = new();

    public InfrastructureMapperTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterTestFixture fixture
    )
        : base(testOutputHelper, fixture)
    {
        _mapper = fixture.GetService<IMapper>(testOutputHelper)!;

        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.AddExtraSettings(jsonSettings =>
        {
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
        });
    }

    [Fact]
    public async Task Map_MeldingEntity_ToMelding()
    {
        //arrange
        var meldingId = Guid.NewGuid();
        var mainDocumentEntity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            MeldingId = meldingId,
            InternalDocumentReference = "internal-id",
            DocumentType = DocumentType.MainContent,
            ScanResult = DocumentScanResult.Clean,
        };
        var structuredDocumentEntity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            MeldingId = meldingId,
            InternalDocumentReference = "internal-structured-id",
            DocumentType = DocumentType.StructuredData,
            ScanResult = DocumentScanResult.Clean,
        };
        var attachmentEntity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            MeldingId = meldingId,
            InternalDocumentReference = "internal-attachment-id",
            DocumentType = DocumentType.Attachment,
            ScanResult = DocumentScanResult.Clean,
        };
        var meldingEntity = new MeldingEntity
        {
            Id = meldingId,
            Source = MessageSource.Api,
            ApplicationId = "app-id",
            ReceivedAt = DateTime.Now,
            Tags = new Dictionary<string, string> { { "tag1", "value1" }, { "tag2", "value2" } },
            InternalTags = new Dictionary<string, string>
            {
                { "internalTag1", "internalValue1" },
                { "internalTag2", "internalValue2" },
            },
            Documents = [mainDocumentEntity, structuredDocumentEntity, attachmentEntity],
        };
        //act
        var result = _mapper.Map<Melding>(meldingEntity);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task Map_DocumentEntity_ToDocument()
    {
        var mainDocumentEntity = new DocumentEntity
        {
            Id = Guid.NewGuid(),
            MeldingId = Guid.NewGuid(),
            ContentType = "application/pdf",
            FileName = "main-content.pdf",
            InternalDocumentReference = "internal-id",
            DocumentType = DocumentType.MainContent,
            ScanResult = DocumentScanResult.Clean,
        };
        //act
        var result = _mapper.Map<Document>(mainDocumentEntity);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task Map_AltinnSubscriptionEntity_ToAltinnConnection()
    {
        //arrange
        var altinnSubscriptionEntity = new AltinnSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            AppIdentifier = "app-identifier",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            SubscriptionId = 12341234,
            SubscriptionEntityId = Guid.NewGuid(),
        };
        //act
        var result = _mapper.Map<AltinnConnection>(altinnSubscriptionEntity);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task Map_SubscriptionEntity_ToConsumerManifest()
    {
        //arrange
        var subscriptionId = Guid.NewGuid();
        var altinnApp1 = new AltinnSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            AppIdentifier = "altinn-app-1",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            SubscriptionId = 111,
            SubscriptionEntityId = subscriptionId,
        };
        var altinnApp2 = new AltinnSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            AppIdentifier = "altinn-app-2",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            SubscriptionId = 222,
            SubscriptionEntityId = subscriptionId,
        };
        var apiApp1 = new ApiSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            AppIdentifier = "api-app-1",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            SubscriptionEntityId = subscriptionId,
        };
        var subscriptionEntity = new SubscriptionEntity
        {
            Id = subscriptionId,
            ConsumerName = "test-consumer",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            RegisteredAltinnApps = [altinnApp1, altinnApp2],
            RegisteredApiApps = [apiApp1],
        };
        //act
        var result = _mapper.Map<ConsumerManifest>(subscriptionEntity);
        //assert
        await Verify(result, _verifySettings);
    }
}
