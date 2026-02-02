using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Argon;
using Bogus;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.Db;

public class MeldingerRepositoryTests : TestBed<InfrastructureAdapterReadOnlyTestFixtureWithDb>
{
    private const int SeedSize = 50;

    private static readonly Faker<MeldingEntity> MeldingEntityFaker = new Faker<MeldingEntity>()
        .UseSeed(1337)
        .RuleForType(typeof(Guid), faker => Guid.NewGuid())
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
                        Id = Guid.NewGuid(),
                        MeldingId = current.Id,
                        InternalDocumentReference = "",
                        DocumentType = DocumentType.MainContent,
                        ScanResult = DocumentScanResult.Clean,
                    },
                    new DocumentEntity()
                    {
                        Id = Guid.NewGuid(),
                        MeldingId = current.Id,
                        InternalDocumentReference = "",
                        DocumentType = DocumentType.StructuredData,
                        ScanResult = DocumentScanResult.Clean,
                    },
                    new DocumentEntity
                    {
                        Id = Guid.NewGuid(),
                        MeldingId = current.Id,
                        InternalDocumentReference = "",
                        DocumentType = DocumentType.Attachment,
                        ScanResult = DocumentScanResult.Clean,
                    }
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
        var result = await _meldingRepository.GetMeldingAsync(existingMeldingGuid);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task GetMelding_WhenCalledWithNonExistingId_ReturnsNull()
    {
        //arrange
        var nonExistingMeldingGuid = Guid.NewGuid();
        //act
        var result = await _meldingRepository.GetMeldingAsync(nonExistingMeldingGuid);
        //assert
        result.ShouldBeNull();
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
        var result = await _meldingRepository.GetMeldingerAsync(pageSize, pageNumber);
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
        var result = await _meldingRepository.GetMeldingerAsync(10);
        //assert
        Seed.OrderByDescending(s => s.ReceivedAt)
            .ThenBy(s => s.Id)
            .Take(10)
            .Select(s => s.Id)
            .ToList()
            .ShouldBeEquivalentTo(result.Items.Select(s => s.Id).ToList());
    }
}
