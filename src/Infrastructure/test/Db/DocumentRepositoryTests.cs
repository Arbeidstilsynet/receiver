using Arbeidstilsynet.Common.TestExtensions.Snapshots;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db;

public class DocumentRepositoryTests : TestBed<InfrastructureAdapterReadOnlyTestFixtureWithDb>
{
    private IDocumentRepository _documentRepository;
    private ReceiverDbContext _dbContext;
    private readonly SnapshotSettings _snapshotSettings = new SnapshotSettings()
        .UseDirectory("Snapshots")
        .IncludeDefaultValues();

    public DocumentRepositoryTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterReadOnlyTestFixtureWithDb fixtureWithDb
    )
        : base(testOutputHelper, fixtureWithDb)
    {
        _documentRepository = fixtureWithDb.GetService<IDocumentRepository>(testOutputHelper)!;
        _dbContext = fixtureWithDb.GetService<ReceiverDbContext>(testOutputHelper)!;
    }

    [Fact]
    public async Task GetAllDocumentsForMelding_WhenCalledWithExistingMeldingId_ReturnsCorrectDocumentReferences()
    {
        //arrange
        var meldingId = MeldingerRepositoryTests.Seed.First().Id;
        //act
        var result = await _documentRepository.GetAllDocumentsForMelding(
            meldingId,
            TestContext.Current.CancellationToken
        );
        //assert
        await Snapshot.Verify(result, _snapshotSettings);
    }

    [Fact]
    public async Task GetAllDocumentsForMelding_WhenCalledWithNonExistingMeldingId_ReturnsEmptyList()
    {
        //arrange
        var meldingId = Guid.NewGuid();
        //act
        var result = await _documentRepository.GetAllDocumentsForMelding(
            meldingId,
            TestContext.Current.CancellationToken
        );
        //assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDocumentAsync_WhenCalledWithExistingMeldingId_ReturnsCorrectDocument()
    {
        //arrange
        var documentId = MeldingerRepositoryTests.Seed.First().Documents[0].Id;
        //act
        var result = await _documentRepository.GetDocumentAsync(
            documentId,
            TestContext.Current.CancellationToken
        );
        //assert
        await Snapshot.Verify(result, _snapshotSettings);
    }

    [Fact]
    public async Task GetDocumentAsync_WhenCalledWithNonExistingDocumentId_ReturnsNull()
    {
        //arrange
        var documentId = Guid.NewGuid();
        //act
        var result = await _documentRepository.GetDocumentAsync(
            documentId,
            TestContext.Current.CancellationToken
        );
        //assert
        result.ShouldBeNull();
    }
}
