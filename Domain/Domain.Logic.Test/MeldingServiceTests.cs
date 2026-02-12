using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test.fixtures;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Domain.Logic.Test.fixtures;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using CreateMeldingRequest = Arbeidstilsynet.MeldingerReceiver.API.Ports.CreateMeldingRequest;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test;

public class MeldingServiceTests : TestBed<DomainLogicTestFixture>
{
    private static readonly CreateMeldingRequest SampleMeldingRequest = new()
    {
        MeldingId = Guid.NewGuid(),
        Source = MessageSource.Altinn,
        ApplicationReference = "test-app",
        MainContent = new UploadDocumentRequest
        {
            FileMetadata = new FileMetadata
            {
                FileName = "main-content.pdf",
                ContentType = "application/pdf",
            },
            InputStream = new MemoryStream(),
        },
        StructuredData = new UploadDocumentRequest()
        {
            FileMetadata = new FileMetadata
            {
                FileName = "structured-data.json",
                ContentType = "application/json",
            },
            InputStream = new MemoryStream(),
        },
    };

    private readonly IDocumentStorage _documentStorage = Substitute.For<IDocumentStorage>();

    private readonly ILogger<MeldingService> _logger = Substitute.For<ILogger<MeldingService>>();

    private readonly IMeldingRepository _meldingRepository = Substitute.For<IMeldingRepository>();

    private readonly IPostMeldingPersistedAction _postMeldingPersistedAction =
        Substitute.For<IPostMeldingPersistedAction>();

    private readonly MeldingService _sut;
    private readonly IVirusScanService _virusScanService = Substitute.For<IVirusScanService>();

    private readonly ISubscriptionService _subscriptionService =
        Substitute.For<ISubscriptionService>();

    public MeldingServiceTests(ITestOutputHelper testOutputHelper, DomainLogicTestFixture fixture)
        : base(testOutputHelper, fixture)
    {
        _postMeldingPersistedAction.Name.Returns("TestAction");

        var mapper = fixture.GetService<IMapper>(testOutputHelper)!;
        _subscriptionService
            .ShouldMeldingForAppIdBeIgnored(Arg.Any<MessageSource>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));
        _sut = new MeldingService(
            _documentStorage,
            _meldingRepository,
            _virusScanService,
            _subscriptionService,
            [_postMeldingPersistedAction],
            mapper,
            _logger
        );
    }

    [Fact]
    public async Task GetMelding_WhenCalledWithGetMeldingRequest_CallsRepositoryMethodWithCorrectId()
    {
        //arrange
        var request = new GetMeldingRequest { MeldingId = Guid.NewGuid() };
        //act
        await _sut.GetMelding(request, TestContext.Current.CancellationToken);
        //assert
        await _meldingRepository
            .Received(1)
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMeldinger_WhenCalledWithDefaultParameters_CallsRepositoryMethodWithCorrectParameters()
    {
        //arrange
        _meldingRepository
            .GetMeldinger(10)
            .Returns(
                new Infrastructure.Ports.Dto.PaginationResponse<Melding>
                {
                    PageNumber = 1,
                    PageSize = 10,
                    TotalPages = 1,
                    TotalRecords = 0,
                    Items = [],
                }
            );
        //act
        var result = await _sut.GetMeldinger();
        //assert
        await _meldingRepository.Received(1).GetMeldinger(10);
        result.ShouldBeEquivalentTo(
            new API.Ports.PaginationResponse<Melding>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalPages = 1,
                TotalRecords = 0,
                Items = [],
            }
        );
    }

    [Fact]
    public async Task ProcessMelding_WhenCalledWithNewPostMeldingRequest_CallsAllRequiredBackendServices()
    {
        //arrange
        var request = SampleMeldingRequest with
        {
            Attachments =
            [
                new UploadDocumentRequest
                {
                    FileMetadata = new FileMetadata
                    {
                        FileName = "attachment.pdf",
                        ContentType = "application/pdf",
                    },
                    InputStream = new MemoryStream(),
                },
            ],
        };
        var mainContentUploadResponse = new UploadResponse
        {
            PersistedDocument = new DocumentStorageDto
            {
                DocumentId = Guid.NewGuid(),
                InternalDocumentReference = "/a/b/c",
                FileName = request.MainContent!.FileMetadata.FileName,
                ContentType = request.MainContent.FileMetadata.ContentType,
                ScanResult = request.MainContent.ScanResult,
            },
        };
        var structuredDataUploadResponse = new UploadResponse
        {
            PersistedDocument = new DocumentStorageDto
            {
                DocumentId = Guid.NewGuid(),
                InternalDocumentReference = "/a/b/structured",
                FileName = request.StructuredData!.FileMetadata.FileName,
                ContentType = request.StructuredData.FileMetadata.ContentType,
                ScanResult = request.StructuredData.ScanResult,
            },
        };
        var attachmentUploadResponse = new UploadResponse
        {
            PersistedDocument = new DocumentStorageDto
            {
                DocumentId = Guid.NewGuid(),
                InternalDocumentReference = "/a/b/d",
                FileName = request.Attachments[0].FileMetadata.FileName,
                ContentType = request.Attachments[0].FileMetadata.ContentType,
                ScanResult = request.Attachments[0].ScanResult,
            },
        };
        _documentStorage
            .Upload(
                Arg.Is<UploadRequest>(i => i.InputStream == request.MainContent.InputStream),
                Arg.Any<CancellationToken>()
            )
            .Returns(mainContentUploadResponse);
        _documentStorage
            .Upload(
                Arg.Is<UploadRequest>(i => i.InputStream == request.StructuredData.InputStream),
                Arg.Any<CancellationToken>()
            )
            .Returns(structuredDataUploadResponse);
        _documentStorage
            .Upload(
                Arg.Is<UploadRequest>(i => i.InputStream == request.Attachments[0].InputStream),
                Arg.Any<CancellationToken>()
            )
            .Returns(attachmentUploadResponse);
        //act
        await _sut.ProcessMelding(request, TestContext.Current.CancellationToken);
        //assert
        await _documentStorage
            .Received(3)
            .Upload(Arg.Any<UploadRequest>(), Arg.Any<CancellationToken>());
        await _meldingRepository
            .Received(1)
            .CreateMelding(
                Arg.Is<Infrastructure.Ports.Dto.CreateMeldingRequest>(i =>
                    i.Id == request.MeldingId
                    && i.ApplicationId == request.ApplicationReference
                    && i.Source == request.Source
                    && i.MainDocumentData == mainContentUploadResponse.PersistedDocument
                    && i.StructuredData == structuredDataUploadResponse.PersistedDocument
                    && i.AttachmentData[0] == attachmentUploadResponse.PersistedDocument
                    && i.Tags == request.Metadata
                ),
                Arg.Any<CancellationToken>()
            );
        await _postMeldingPersistedAction.Received(1).RunPostActionFor(Arg.Any<Melding>());
    }

    [Fact]
    public async Task ProcessMelding_WhenCalledWithAlreadyPersistedMelding_OnlyCallsPostMeldingActions()
    {
        //arrange
        var existingRequest = SampleMeldingRequest with
        {
            MeldingId = Guid.NewGuid(),
        };
        _meldingRepository
            .GetMelding(existingRequest.MeldingId, Arg.Any<CancellationToken>())
            .Returns(Substitute.For<Melding>());
        //act
        await _sut.ProcessMelding(existingRequest, TestContext.Current.CancellationToken);
        //assert
        await _documentStorage.DidNotReceiveWithAnyArgs().Upload(default!, default!);
        await _meldingRepository.DidNotReceiveWithAnyArgs().CreateMelding(default!, default!);
        await _postMeldingPersistedAction.Received(1).RunPostActionFor(Arg.Any<Melding>());
    }

    [Fact]
    public async Task ProcessMelding_WhenPostMeldingActionThrowsException_ShouldNotThrow()
    {
        // Arrange
        var request = SampleMeldingRequest;
        _postMeldingPersistedAction
            .RunPostActionFor(Arg.Any<Melding>())
            .Throws(new Exception("Test exception"));

        _meldingRepository
            .CreateMelding(default!, default!)
            .ReturnsForAnyArgs(TestData.CreateMeldingFaker().Generate());

        _documentStorage
            .Upload(
                Arg.Is<UploadRequest>(i => i.InputStream == request.MainContent!.InputStream),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new UploadResponse
                {
                    PersistedDocument = new DocumentStorageDto
                    {
                        DocumentId = Guid.NewGuid(),
                        InternalDocumentReference = "/a/b/c",
                        ContentType = SampleMeldingRequest.MainContent!.FileMetadata.ContentType,
                        FileName = SampleMeldingRequest.MainContent.FileMetadata.FileName,
                        ScanResult = SampleMeldingRequest.MainContent.ScanResult,
                    },
                }
            );

        // Act & Assert
        _ = _sut.ProcessMelding(request, TestContext.Current.CancellationToken).ShouldNotThrow();

        await _postMeldingPersistedAction.Received(1).RunPostActionFor(Arg.Any<Melding>());
    }

    [Fact]
    public async Task ProcessMelding_WhenCalledWithNoContent_ShouldPersistMelding()
    {
        //arrange
        var request = SampleMeldingRequest with
        {
            MainContent = null,
            StructuredData = null,
            Attachments = [],
        };
        //act
        await _sut.ProcessMelding(request, TestContext.Current.CancellationToken);
        //assert
        await _meldingRepository
            .Received(1)
            .CreateMelding(
                Arg.Is<Infrastructure.Ports.Dto.CreateMeldingRequest>(i =>
                    i.Id == request.MeldingId
                    && i.ApplicationId == request.ApplicationReference
                    && i.Source == request.Source
                    && i.MainDocumentData == null
                    && i.StructuredData == null
                    && i.AttachmentData.Count == 0
                    && i.Tags == request.Metadata
                ),
                Arg.Any<CancellationToken>()
            );
    }
}
