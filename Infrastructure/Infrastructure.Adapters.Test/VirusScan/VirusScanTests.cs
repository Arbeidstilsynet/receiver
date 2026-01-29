using System.Text;
using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.VirusScan;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Shouldly;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.VirusScan;

public class VirusScanTests
{
    private readonly IOptions<InfrastructureConfiguration> _options;

    private readonly IDocumentStorage _documentStorage;
    private readonly WireMockServer _server;

    private VirusScanService _sut;

    public VirusScanTests()
    {
        _server = WireMockServer.Start();
        PrepareMockServer();
        _options = Options.Create(Substitute.For<InfrastructureConfiguration>());
        var httpClientFactory = PrepareHttpClientMock();
        _documentStorage = Substitute.For<IDocumentStorage>();
        // sut
        _sut = new VirusScanService(
            httpClientFactory,
            _documentStorage,
            Substitute.For<ILogger<VirusScanService>>(),
            _options
        );
    }

    private const string CleanTestMessage = "Hello, Stream!";

    private const string MaliciousTestMessage = ":-o";

    private static MimePartMatcher TestMessageMultiPartMatcher(string bodyContent) =>
        new(
            MatchBehaviour.AcceptOnMatch,
            new ContentTypeMatcher("text/plain"),
            new WildcardMatcher("form-data; name=\"file\"; filename=\"test.txt\"*"),
            null,
            new ExactMatcher(bodyContent)
        );

    private IHttpClientFactory PrepareHttpClientMock()
    {
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient();
        httpClientFactory.CreateClient("VirusScanServiceClient").Returns(httpClient);

        _options.Value.VirusScanConfiguration.Returns(
            new VirusScanConfiguration() { BaseUrl = $"{_server.Urls[0]}" }
        );
        return httpClientFactory;
    }

    private void PrepareMockServer()
    {
        _server
            .Given(
                Request
                    .Create()
                    .WithPath("/api/v2/scan")
                    .WithMultiPart(TestMessageMultiPartMatcher(CleanTestMessage))
                    .UsingPost()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBody(
                        JsonSerializer.Serialize<ScanResponse[]>([
                            new ScanResponse { Result = Status.OK, Filename = "test.txt" },
                        ])
                    )
            );

        _server
            .Given(
                Request
                    .Create()
                    .WithPath("/api/v2/scan")
                    .WithMultiPart(TestMessageMultiPartMatcher(MaliciousTestMessage))
                    .UsingPost()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBody(
                        JsonSerializer.Serialize<ScanResponse[]>([
                            new ScanResponse { Result = Status.FOUND, Filename = "test.txt" },
                        ])
                    )
            );
    }

    [Theory]
    [InlineData(CleanTestMessage, DocumentScanResult.Clean)]
    [InlineData(MaliciousTestMessage, DocumentScanResult.Infected)]
    public async Task ScanForVirus_UpdatesScanResultBasedOnScanResponseStatus(
        string content,
        DocumentScanResult expectedStatus
    )
    {
        //arrange
        _documentStorage.ClearSubstitute();
        var uploadResponse = new UploadResponse
        {
            PersistedDocument = new DocumentStorageDto
            {
                DocumentId = Guid.NewGuid(),
                InternalDocumentReference = "path/to/file",
                ContentType = "text/plain",
                FileName = "test.txt",
                ScanResult = DocumentScanResult.Unknown,
            },
        };

        _documentStorage
            .When(x =>
                x.Download(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            )
            .Do(callInfo =>
            {
                var writer = new StreamWriter(callInfo.Arg<Stream>(), new UTF8Encoding(false));
                writer.Write(content);
                writer.Flush(); //otherwise you are risking empty stream
            });

        //act
        await _sut.ScanForVirus(uploadResponse, TestContext.Current.CancellationToken);
        //assert
        uploadResponse.PersistedDocument.ScanResult.ShouldBe(expectedStatus);
    }
}
