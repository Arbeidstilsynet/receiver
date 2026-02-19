using System.Text;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Storage;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.Storage;

public class DocumentStorageTests : TestBed<InfrastructureAdapterTestFixtureWithStorage>
{
    private readonly IDocumentStorage _documentStorage;

    public DocumentStorageTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterTestFixtureWithStorage fixture
    )
        : base(testOutputHelper, fixture)
    {
        _documentStorage = fixture.GetService<IDocumentStorage>(testOutputHelper)!;
    }

    [Fact]
    public async Task Upload_WithValidRequest_StoresFileAndReturnsReference()
    {
        //arrange
        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("blub"));
        var uploadRequest = new UploadRequest
        {
            Document = new Document
            {
                DocumentId = Guid.NewGuid(),
                MeldingId = Guid.NewGuid(),
                FileMetadata = new FileMetadata
                {
                    ContentType = "text/plain",
                    FileName = "upload-test.txt",
                },
                ScanResult = DocumentScanResult.Clean,
            },
            InputStream = inputStream,
        };
        //act
        var documentId = await _documentStorage.Upload(
            uploadRequest,
            TestContext.Current.CancellationToken
        );
        //assert
        documentId.PersistedDocument.InternalDocumentReference.ShouldBe(
            uploadRequest.CreateGcsObjectName()
        );
    }

    [Fact]
    public async Task Download_WithValidIds_ReturnsStream()
    {
        //arrange
        using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("blub"));
        var uploadRequest = new UploadRequest
        {
            Document = new Document
            {
                MeldingId = Guid.NewGuid(),
                DocumentId = Guid.NewGuid(),
                FileMetadata = new FileMetadata
                {
                    ContentType = "text/plain",
                    FileName = "download-test.txt",
                },
                ScanResult = DocumentScanResult.Clean,
            },
            InputStream = inputStream,
        };
        //act
        var document = await _documentStorage.Upload(
            uploadRequest,
            TestContext.Current.CancellationToken
        );
        using var responseStream = new MemoryStream();

        await _documentStorage.Download(
            document.PersistedDocument.InternalDocumentReference,
            responseStream,
            TestContext.Current.CancellationToken
        );

        //assert
        Encoding.UTF8.GetString(responseStream.ToArray()).ShouldBe("blub");
    }
}
