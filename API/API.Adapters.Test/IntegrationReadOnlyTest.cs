using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test.fixture;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Model.Response;
using Argon;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test;

public class IntegrationReadOnlyTest : IClassFixture<ApplicationFixture>
{
    private readonly HttpClient _client;
    private readonly VerifySettings _verifySettings = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public IntegrationReadOnlyTest(ApplicationFixture factory)
    {
        _client = factory.CreateClient();
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.AddExtraSettings(jsonSettings =>
        {
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
        });
    }

    [Fact]
    public async Task GetMeldinger_ReturnsAllMeldinger()
    {
        // Act
        var content = await _client.GetFromJsonAsync<GetAllMeldingerResponse>(
            "/meldinger",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        await Verify(content, _verifySettings);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 10)]
    public async Task GetMeldinger_ValidRequest_ReturnsPagedMeldinger(int pageNumber, int pageSize)
    {
        // Act
        var response = await _client.GetAsync(
            $"/meldinger?pageNumber={pageNumber}&pageSize={pageSize}",
            TestContext.Current.CancellationToken
        );

        var content = await response.Content.ReadFromJsonAsync<GetAllMeldingerResponse>(
            _jsonSerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        await Verify(content, _verifySettings).UseParameters(pageNumber, pageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    [InlineData(1, 0)]
    public async Task GetMeldinger_InvalidRequest_ReturnsBadRequest(int pageNumber, int pageSize)
    {
        // Act
        var response = await _client.GetAsync(
            $"/meldinger?pageNumber={pageNumber}&pageSize={pageSize}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMelding_ReturnsMelding()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;

        // Act
        var content = await _client.GetFromJsonAsync<GetMeldingResponse>(
            $"/meldinger/{meldingId}",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        await Verify(content, _verifySettings);
    }

    [Fact]
    public async Task GetMelding_ReturnsNotFoundForUnknownMelding()
    {
        // Arrange
        var unknownMeldingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{unknownMeldingId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDocuments_ReturnsDocumentsForMelding()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;

        // Act
        var content = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        await Verify(content, _verifySettings);
    }

    [Fact]
    public async Task GetDocuments_ReturnsNotFoundForUnknownMelding()
    {
        // Arrange
        var unknownMeldingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{unknownMeldingId}/documents",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDocument_ReturnsAttachment()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;
        var documentId = ApplicationFixture.KnownAttachmentDocumentId;

        // Act
        var response = await _client.GetFromJsonAsync<Document>(
            $"/meldinger/{meldingId}/documents/{documentId}",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        await Verify(response, _verifySettings);
    }
    
    [Fact]
    public async Task GetDocument_ReturnsMainContent()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;
        var documentId = ApplicationFixture.KnownMeldingId; // Main content uses MeldingId as DocumentId

        // Act
        var response = await _client.GetFromJsonAsync<Document>(
            $"/meldinger/{meldingId}/documents/{documentId}",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        await Verify(response, _verifySettings);
    }
    
    [Fact]
    public async Task GetDocument_ReturnsStructuredData()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;
        var documentId = ApplicationFixture.KnownStructuredDataId;

        // Act
        var response = await _client.GetFromJsonAsync<Document>(
            $"/meldinger/{meldingId}/documents/{documentId}",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        await Verify(response, _verifySettings);
    }

    [Fact]
    public async Task GetDocument_ReturnsNotFoundForUnknownDocument()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;
        var unknownDocumentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{meldingId}/documents/{unknownDocumentId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDocument_ReturnsNotFoundForUnknownMelding()
    {
        // Arrange
        var unknownMeldingId = Guid.NewGuid();
        var documentId = ApplicationFixture.KnownAttachmentDocumentId;

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{unknownMeldingId}/documents/{documentId}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadDocument_ReturnsDocument()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;
        var documentId = ApplicationFixture.KnownAttachmentDocumentId;

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{meldingId}/documents/{documentId}/download",
            TestContext.Current.CancellationToken
        );

        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );

        await Verify(content, _verifySettings);
    }

    [Fact]
    public async Task DownloadDocument_ReturnsNotFoundForUnknownDocument()
    {
        // Arrange
        var meldingId = ApplicationFixture.KnownMeldingId;
        var unknownDocumentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{meldingId}/documents/{unknownDocumentId}/download",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadDocument_ReturnsNotFoundForUnknownMelding()
    {
        // Arrange
        var unknownMeldingId = Guid.NewGuid();
        var documentId = ApplicationFixture.KnownAttachmentDocumentId;

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/{unknownMeldingId}/documents/{documentId}/download",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllSubscriptions_ReturnsAllSubscriptions()
    {
        // Act
        var content = await _client.GetFromJsonAsync<List<ConsumerManifest>>(
            "/subscriptions",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        await Verify(content, _verifySettings);
    }
}
