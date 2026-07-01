using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Argon;
using Bogus;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db;

public class MeldingerRepositoryTests : TestBed<InfrastructureAdapterReadOnlyTestFixtureWithDb>
{
    private const int SeedSize = 50;

    private static readonly Faker<MeldingEntity> MeldingEntityFaker = new Faker<MeldingEntity>()
        .UseSeed(1337)
        .RuleForType(typeof(Guid), faker => faker.Random.Guid())
        .RuleFor(x => x.Source, faker => faker.PickRandom(MessageSource.Altinn, MessageSource.Api))
        .RuleForType(
            typeof(DateTime),
            faker => faker.Date.PastDateOnly(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        )
        .RuleFor(x => x.ApplicationId, faker => "test-app")
        .RuleFor(
            x => x.Documents,
            (faker, current) =>
                [
                    new DocumentEntity
                    {
                        Id = faker.Random.Guid(),
                        MeldingId = current.Id,
                        InternalDocumentReference = "",
                        DocumentType = DocumentType.MainContent,
                        ScanResult = DocumentScanResult.Clean,
                        Tags = new Dictionary<string, string>()
                        {
                            { "key1", "value1" },
                            { "AltinnId", faker.Random.Guid().ToString() },
                            { "AltinnDataType", "ref-data-as-pdf" },
                        },
                    },
                    new DocumentEntity()
                    {
                        Id = faker.Random.Guid(),
                        MeldingId = current.Id,
                        InternalDocumentReference = "",
                        DocumentType = DocumentType.StructuredData,
                        ScanResult = DocumentScanResult.Clean,
                        Tags = new Dictionary<string, string>()
                        {
                            { "AltinnId", faker.Random.Guid().ToString() },
                            { "AltinnDataType", "skjema" },
                        },
                    },
                    new DocumentEntity
                    {
                        Id = faker.Random.Guid(),
                        MeldingId = current.Id,
                        InternalDocumentReference = "",
                        DocumentType = DocumentType.Attachment,
                        ScanResult = DocumentScanResult.Clean,
                        Tags = new Dictionary<string, string>()
                        {
                            { "AltinnId", faker.Random.Guid().ToString() },
                            { "AltinnDataType", "vedlegg" },
                        },
                    },
                ]
        );

    internal static List<MeldingEntity> Seed = MeldingEntityFaker.Generate(SeedSize);
    private readonly VerifySettings _verifySettings = new();
    private readonly IMeldingRepository _meldingRepository;

    public MeldingerRepositoryTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterReadOnlyTestFixtureWithDb fixtureWithDb
    )
        : base(testOutputHelper, fixtureWithDb)
    {
        _meldingRepository = fixtureWithDb.GetService<IMeldingRepository>(testOutputHelper)!;

        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.AddExtraSettings(jsonSettings =>
        {
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
        });
    }

    [Fact]
    public async Task GetMelding_WhenCalledWithExistingId_ReturnsMelding()
    {
        //arrange
        var existingMeldingGuid = Seed.First().Id;
        //act
        var result = await _meldingRepository.GetMelding(
            existingMeldingGuid,
            TestContext.Current.CancellationToken
        );
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task GetMelding_WhenCalledWithNonExistingId_ReturnsNull()
    {
        //arrange
        var nonExistingMeldingGuid = Guid.NewGuid();
        //act
        var result = await _meldingRepository.GetMelding(
            nonExistingMeldingGuid,
            TestContext.Current.CancellationToken
        );
        //assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetMeldingerByShortId_WhenCalledWithExistingShortId_ReturnsMatchingMelding()
    {
        //arrange
        var existingMelding = Seed.First();
        var shortId = existingMelding.Id.ToString().Split('-').Last();
        //act
        var result = await _meldingRepository.GetMeldingerByShortId(
            shortId,
            TestContext.Current.CancellationToken
        );
        //assert
        result.ShouldContain(m => m.Id == existingMelding.Id);
    }

    [Fact]
    public async Task GetMeldingerByShortId_IsCaseInsensitive()
    {
        //arrange
        var existingMelding = Seed.First();
        var shortId = existingMelding.Id.ToString().Split('-').Last().ToUpperInvariant();
        //act
        var result = await _meldingRepository.GetMeldingerByShortId(
            shortId,
            TestContext.Current.CancellationToken
        );
        //assert
        result.ShouldContain(m => m.Id == existingMelding.Id);
    }

    [Fact]
    public async Task GetMeldingerByShortId_WhenCalledWithNonExistingShortId_ReturnsEmpty()
    {
        //arrange
        var nonExistingShortId = Guid.NewGuid().ToString().Split('-').Last();
        //act
        var result = await _meldingRepository.GetMeldingerByShortId(
            nonExistingShortId,
            TestContext.Current.CancellationToken
        );
        //assert
        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, 10, 5, 10)]
    [InlineData(2, 10, 5, 10)]
    [InlineData(3, 10, 5, 10)]
    [InlineData(4, 10, 5, 10)]
    [InlineData(5, 10, 5, 10)]
    [InlineData(6, 10, 5, 0)]
    [InlineData(1, 100, 1, 50)]
    [InlineData(1, 30, 2, 30)]
    [InlineData(2, 30, 2, 20)]
    public async Task GetMeldinger_WhenCalledWithPageNumberAndPageSize_ReturnsExpectedPaginationResult(
        int pageNumber,
        int pageSize,
        int expectedTotalPages,
        int expectedItemCount
    )
    {
        //arrange
        //act
        var result = await _meldingRepository.GetMeldinger(pageSize, pageNumber);
        //assert
        result.PageSize.ShouldBe(pageSize);
        result.TotalPages.ShouldBe(expectedTotalPages);
        result.PageNumber.ShouldBe(pageNumber);
        result.TotalRecords.ShouldBe(SeedSize);
        result.Items.Count().ShouldBe(expectedItemCount);
    }

    [Fact]
    public async Task GetMeldinger_WhenCalledWithDefaultParameters_ReturnsFirstBatchOfExistingMeldingerSorted()
    {
        //arrange
        //act
        var result = await _meldingRepository.GetMeldinger(10);
        //assert
        Seed.OrderByDescending(s => s.ReceivedAt)
            .ThenBy(s => s.Id)
            .Take(10)
            .Select(s => s.Id)
            .ToList()
            .ShouldBeEquivalentTo(result.Items.Select(s => s.Id).ToList());
    }
}
