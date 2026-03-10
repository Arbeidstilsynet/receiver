using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test;

public class DocumentServiceTests
{
    private static readonly Melding SampleMelding = new()
    {
        Id = default,
        Source = MessageSource.Altinn,
        ApplicationId = "test",
        ReceivedAt = default,
    };

    private static readonly Document SampleDocument = new()
    {
        DocumentId = default,
        MeldingId = default,
        FileMetadata = new FileMetadata { FileName = "test.txt", ContentType = "text/plain" },
        ScanResult = DocumentScanResult.Clean,
    };

    private readonly IDocumentRepository _documentRepository =
        Substitute.For<IDocumentRepository>();

    private readonly IDocumentStorage _documentStorage = Substitute.For<IDocumentStorage>();

    private readonly ILogger<DocumentService> _logger = Substitute.For<ILogger<DocumentService>>();

    private readonly IMeldingRepository _meldingRepository = Substitute.For<IMeldingRepository>();

    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _sut = new DocumentService(
            _meldingRepository,
            _documentRepository,
            _documentStorage,
            _logger
        );
    }

    [Fact]
    public async Task GetDocument_WhenCalledWithCorrectParametersForMainDocument_CallsDocumentRepository()
    {
        //arrange
        var request = new GetDocumentRequest
        {
            MeldingId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
        };
        _meldingRepository
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns(
                SampleMelding with
                {
                    Id = request.MeldingId,
                    MainContentId = request.DocumentId,
                }
            );
        //act
        await _sut.GetDocument(request, CancellationToken.None);
        //assert
        await _documentRepository
            .Received(1)
            .GetDocumentAsync(request.DocumentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocument_WhenCalledWithCorrectParametersForAttachment_CallsDocumentRepository()
    {
        //arrange

        var request = new GetDocumentRequest
        {
            MeldingId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
        };
        _meldingRepository
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns(
                SampleMelding with
                {
                    Id = request.MeldingId,
                    AttachmentIds = [request.DocumentId],
                }
            );
        //act
        await _sut.GetDocument(request, CancellationToken.None);
        //assert
        await _documentRepository
            .Received(1)
            .GetDocumentAsync(request.DocumentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDocument_WhenCalledWithNonMatchingDocumentId_ReturnsNull()
    {
        //arrange

        var request = new GetDocumentRequest
        {
            MeldingId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
        };
        _meldingRepository
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns(SampleMelding with { Id = request.MeldingId });
        //act
        var result = await _sut.GetDocument(request, CancellationToken.None);
        //assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDocument_WhenCalledWithNonMatchingMeldingId_ReturnsNull()
    {
        //arrange

        var request = new GetDocumentRequest
        {
            MeldingId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
        };
        _meldingRepository
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns(SampleMelding with { MainContentId = request.DocumentId });
        //act
        var result = await _sut.GetDocument(request, CancellationToken.None);
        //assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(DocumentScanResult.Infected)]
    [InlineData(DocumentScanResult.Unknown)]
    public async Task GetDocument_WhenDocumentIsNotSafeToUse_ThrowsException(
        DocumentScanResult scanResult
    )
    {
        //arrange
        var request = new GetDocumentRequest
        {
            MeldingId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
        };
        _meldingRepository
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns(
                SampleMelding with
                {
                    Id = request.MeldingId,
                    MainContentId = request.DocumentId,
                }
            );
        _documentRepository
            .GetDocumentAsync(request.DocumentId, Arg.Any<CancellationToken>())
            .Returns(
                SampleDocument with
                {
                    MeldingId = request.MeldingId,
                    DocumentId = request.DocumentId,
                    ScanResult = scanResult,
                }
            );

        //act
        var act = async () => await _sut.GetDocument(request, CancellationToken.None);

        //assert
        await act.ShouldThrowAsync<DocumentNotSafeToUseException>();
    }

    [Theory]
    [InlineData(DocumentScanResult.Infected)]
    [InlineData(DocumentScanResult.Unknown)]
    public async Task GetAllDocuments_WhenDocumentOneIsNotSafeToUse_ExcludesItFromResult(
        DocumentScanResult scanResult
    )
    {
        //arrange
        var request = new GetAllDocumentsRequest { MeldingId = Guid.NewGuid() };

        var safeDocument = SampleDocument with
        {
            MeldingId = request.MeldingId,
            DocumentId = Guid.NewGuid(),
            ScanResult = DocumentScanResult.Clean,
        };

        var unsafeDocument = SampleDocument with
        {
            MeldingId = request.MeldingId,
            DocumentId = Guid.NewGuid(),
            ScanResult = scanResult,
        };

        _meldingRepository
            .GetMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns(SampleMelding with { Id = request.MeldingId });
        _documentRepository
            .GetAllDocumentsForMelding(request.MeldingId, Arg.Any<CancellationToken>())
            .Returns([safeDocument, unsafeDocument]);

        //act
        var documents = (await _sut.GetAllDocuments(request, CancellationToken.None))?.ToList();

        //assert
        documents.ShouldNotBeNull().ShouldBe([safeDocument]);
    }

    [Fact]
    public async Task GetAllDocuments()
    {
        //arrange
        var request = new GetAllDocumentsRequest { MeldingId = Guid.NewGuid() };
        //act
        var result = await _sut.GetAllDocuments(request, CancellationToken.None);
        //assert
    }
}
