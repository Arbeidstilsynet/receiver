using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Argon;
using Bogus;
using MapsterMapper;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.Db;

public class MeldingerRepositoryWriteTests : TestBed<InfrastructureAdapterWriteTestFixtureWithDb>
{
    private readonly VerifySettings _verifySettings = new();

    private readonly IMeldingRepository _meldingRepository;

    public MeldingerRepositoryWriteTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterWriteTestFixtureWithDb fixtureWithDb
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
        var result = await _meldingRepository.CreateMelding(melding);
        //assert
        var savedMelding = await _meldingRepository.GetMeldingAsync(melding.Id);

        savedMelding.ShouldBeEquivalentTo(result);
        await Verify(savedMelding, _verifySettings);
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
        var result = await _meldingRepository.CreateMelding(melding);
        //assert
        var savedMelding = await _meldingRepository.GetMeldingAsync(melding.Id);

        savedMelding.ShouldBeEquivalentTo(result);
        await Verify(savedMelding, _verifySettings);
    }
}
