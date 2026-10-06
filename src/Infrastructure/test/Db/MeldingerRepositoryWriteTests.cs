using Arbeidstilsynet.Common.TestExtensions.Snapshots;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db;

public class MeldingerRepositoryWriteTests : TestBed<InfrastructureAdapterWriteTestFixtureWithDb>
{
    private readonly SnapshotSettings _snapshotSettings = new SnapshotSettings()
        .UseDirectory("Snapshots")
        .IncludeDefaultValues();

    private readonly IMeldingRepository _meldingRepository;

    public MeldingerRepositoryWriteTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterWriteTestFixtureWithDb fixtureWithDb
    )
        : base(testOutputHelper, fixtureWithDb)
    {
        _meldingRepository = fixtureWithDb.GetService<IMeldingRepository>(testOutputHelper)!;
    }

    [Fact]
    public async Task SaveMelding_WhenCalledWithMeldingDto_PersistsEntity()
    {
        //arrange
        var melding = new CreateMeldingRequest
        {
            Id = Guid.NewGuid(),
            ApplicationId = "altinn-app",
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
            MainDocumentData = new DocumentStorageDto
            {
                DocumentId = Guid.NewGuid(),
                InternalDocumentReference = Guid.NewGuid().ToString(),
                ContentType = "content-type",
                FileName = "file1",
                ScanResult = DocumentScanResult.Clean,
            },
            AttachmentData =
            [
                new DocumentStorageDto
                {
                    DocumentId = Guid.NewGuid(),
                    InternalDocumentReference = Guid.NewGuid().ToString(),
                    ContentType = "content-type",
                    FileName = "file2",
                    ScanResult = DocumentScanResult.Clean,
                },
                new DocumentStorageDto
                {
                    DocumentId = Guid.NewGuid(),
                    InternalDocumentReference = Guid.NewGuid().ToString(),
                    ContentType = "content-type",
                    FileName = "file3",
                    ScanResult = DocumentScanResult.Clean,
                },
            ],
        };
        //act
        var result = await _meldingRepository.CreateMelding(
            melding,
            TestContext.Current.CancellationToken
        );
        //assert
        var savedMelding = await _meldingRepository.GetMelding(
            melding.Id,
            TestContext.Current.CancellationToken
        );

        savedMelding.ShouldBeEquivalentTo(result);
        await Snapshot.Verify(savedMelding, _snapshotSettings);
    }

    [Fact]
    public async Task SaveMelding_WhenCalledWithMeldingDtoWithoutMainDocument_PersistsEntity()
    {
        //arrange
        var melding = new CreateMeldingRequest
        {
            Id = Guid.NewGuid(),
            ApplicationId = "altinn-app",
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
            AttachmentData =
            [
                new DocumentStorageDto
                {
                    DocumentId = Guid.NewGuid(),
                    InternalDocumentReference = Guid.NewGuid().ToString(),
                    ContentType = "content-type",
                    FileName = "file2",
                    ScanResult = DocumentScanResult.Clean,
                },
                new DocumentStorageDto
                {
                    DocumentId = Guid.NewGuid(),
                    InternalDocumentReference = Guid.NewGuid().ToString(),
                    ContentType = "content-type",
                    FileName = "file3",
                    ScanResult = DocumentScanResult.Clean,
                },
            ],
        };
        //act
        var result = await _meldingRepository.CreateMelding(
            melding,
            TestContext.Current.CancellationToken
        );
        //assert
        var savedMelding = await _meldingRepository.GetMelding(
            melding.Id,
            TestContext.Current.CancellationToken
        );

        savedMelding.ShouldBeEquivalentTo(result);
        await Snapshot.Verify(savedMelding, _snapshotSettings);
    }
}
