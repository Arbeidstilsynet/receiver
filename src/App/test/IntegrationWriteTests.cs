using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.Common.TestExtensions.Snapshots;
using Arbeidstilsynet.MeldingerReceiver.App.Test.Extensions;
using Arbeidstilsynet.MeldingerReceiver.App.Test.fixture;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test;

public class IntegrationWriteTests : IClassFixture<ApplicationFixture>
{
    private readonly HttpClient _client;
    private readonly ApplicationFixture _fixture;
    private readonly SnapshotSettings _snapshotSettings = new SnapshotSettings()
        .UseDirectory("Snapshots")
        .IncludeDefaultValues();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public IntegrationWriteTests(ApplicationFixture factory)
    {
        _fixture = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostMelding_NoStructuredDataOrMainContent_ReturnsBadRequest()
    {
        var postMeldingBody = CreatePostMeldingBody() with
        {
            MainContent = null,
            StructuredData = null,
            Attachments = [TestData.CreateFormFile("attachment1.txt", "Attachment 1 content")],
        };

        var httpResponse = await _client.PostAsync(
            "/meldinger",
            postMeldingBody.ToMultipartFormDataContent(),
            TestContext.Current.CancellationToken
        );

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMelding_StructuredDataIsNotJson_ReturnsBadRequest()
    {
        var postMeldingBody = CreatePostMeldingBody() with
        {
            StructuredData = TestData.CreateFormFile(
                "structuredData.txt",
                "This is not JSON.. but it should be",
                contentType: "text/plain"
            ),
        };

        var httpResponse = await _client.PostAsync(
            "/meldinger",
            postMeldingBody.ToMultipartFormDataContent(),
            TestContext.Current.CancellationToken
        );

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMelding_MainContentIsJson_ReturnsBadRequest()
    {
        var postMeldingBody = CreatePostMeldingBody() with
        {
            MainContent = TestData.CreateFormFile(
                "mainContent.json",
                "{ \"key\": \"value\" }",
                contentType: "application/json"
            ),
        };

        var httpResponse = await _client.PostAsync(
            "/meldinger",
            postMeldingBody.ToMultipartFormDataContent(),
            TestContext.Current.CancellationToken
        );

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostMelding_ThenGetMelding_ReturnsMelding()
    {
        var postMeldingBody = CreatePostMeldingBody() with
        {
            Metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
            },
            MainContent = TestData.CreateFormFile(
                "mainContent.txt",
                "Hello World",
                contentType: "text/plain"
            ),
            StructuredData = TestData.CreateFormFile(
                "structuredData.json",
                "{ \"structuredKey\": \"structuredValue\" }",
                contentType: "application/json"
            ),
            Attachments =
            [
                TestData.CreateFormFile(
                    "attachment1.txt",
                    "Attachment 1 content",
                    contentType: "text/plain"
                ),
                TestData.CreateFormFile(
                    "attachment2.txt",
                    "Attachment 2 content",
                    contentType: "text/plain"
                ),
            ],
        };

        var httpResponse = await _client.PostAsync(
            "/meldinger",
            postMeldingBody.ToMultipartFormDataContent(),
            TestContext.Current.CancellationToken
        );
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var postMeldingResponse = await httpResponse.Content.ReadFromJsonAsync<PostMeldingResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        postMeldingResponse.ShouldNotBeNull();

        var getMeldingResponse = await _client.GetFromJsonAsync<GetMeldingResponse>(
            $"/meldinger/{postMeldingResponse.MeldingId}",
            _jsonSerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var melding = getMeldingResponse.ShouldNotBeNull().Melding.ShouldNotBeNull();

        melding.ApplicationId.ShouldBe(postMeldingBody.ApplicationId);
        melding.MainContentId.ShouldNotBeNull();
        melding.StructuredDataId.ShouldNotBeNull();
        melding.AttachmentIds.Count.ShouldBe(postMeldingBody.Attachments.Count);
        melding.Tags.ShouldBe(postMeldingBody.Metadata);
    }

    [Fact]
    public async Task DeleteAndRestoreAttachment_HidesDocumentUntilRestored()
    {
        var (meldingId, documentId, mainDocumentId) = await CreateMeldingWithAttachment();
        var documentRoute = $"/meldinger/{meldingId}/documents/{documentId}";

        using var deletion = await _client.DeleteAsync(
            documentRoute,
            TestContext.Current.CancellationToken
        );
        deletion.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var repeatedDeletion = await _client.DeleteAsync(
            documentRoute,
            TestContext.Current.CancellationToken
        );
        repeatedDeletion.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var melding = await _client.GetFromJsonAsync<GetMeldingResponse>(
            $"/meldinger/{meldingId}",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        melding.ShouldNotBeNull().Melding.ShouldNotBeNull().AttachmentIds.ShouldBeEmpty();
        var documents = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        documents.ShouldNotBeNull().Documents.Select(d => d.DocumentId).ShouldBe([mainDocumentId]);
        using var lookup = await _client.GetAsync(
            documentRoute,
            TestContext.Current.CancellationToken
        );
        lookup.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var download = await _client.GetAsync(
            $"{documentRoute}/download",
            TestContext.Current.CancellationToken
        );
        download.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var deleted = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents/deleted",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        var deletedDocument = deleted.ShouldNotBeNull().Documents.ShouldHaveSingleItem();
        deletedDocument.DocumentId.ShouldBe(documentId);
        deletedDocument.DeletedAt.ShouldNotBeNull();

        using var restore = await _client.PostAsync(
            $"{documentRoute}/restore",
            null,
            TestContext.Current.CancellationToken
        );
        restore.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var restoredMelding = await _client.GetFromJsonAsync<GetMeldingResponse>(
            $"/meldinger/{meldingId}",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        restoredMelding
            .ShouldNotBeNull()
            .Melding.ShouldNotBeNull()
            .AttachmentIds.ShouldBe([documentId]);
        var deletedAfterRestore = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents/deleted",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        deletedAfterRestore.ShouldNotBeNull().Documents.ShouldBeEmpty();
        using var restoredDownload = await _client.GetAsync(
            $"{documentRoute}/download",
            TestContext.Current.CancellationToken
        );
        restoredDownload.StatusCode.ShouldBe(HttpStatusCode.OK);
        (
            await restoredDownload.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        ).ShouldBe("preserved blob");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteDocument_MainContentOrStructuredData_ReturnsBadRequest(
        bool structuredData
    )
    {
        var (meldingId, _, protectedId) = await CreateMeldingWithAttachment(
            mainIsStructuredData: structuredData
        );

        using var deletion = await _client.DeleteAsync(
            $"/meldinger/{meldingId}/documents/{protectedId}",
            TestContext.Current.CancellationToken
        );

        deletion.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteOrRestoreDocument_NotOnMelding_ReturnsNotFound()
    {
        var (meldingId, _, _) = await CreateMeldingWithAttachment();
        var (_, foreignDocumentId, _) = await CreateMeldingWithAttachment();

        foreach (var documentId in new[] { Guid.NewGuid(), foreignDocumentId })
        {
            var documentRoute = $"/meldinger/{meldingId}/documents/{documentId}";
            using var deletion = await _client.DeleteAsync(
                documentRoute,
                TestContext.Current.CancellationToken
            );
            deletion.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            using var restore = await _client.PostAsync(
                $"{documentRoute}/restore",
                null,
                TestContext.Current.CancellationToken
            );
            restore.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        using var deletedForUnknownMelding = await _client.GetAsync(
            $"/meldinger/{Guid.NewGuid()}/documents/deleted",
            TestContext.Current.CancellationToken
        );
        deletedForUnknownMelding.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(
        Guid MeldingId,
        Guid DocumentId,
        Guid MainDocumentId
    )> CreateMeldingWithAttachment(bool mainIsStructuredData = false)
    {
        var meldingId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var mainDocumentId = Guid.NewGuid();
        using var scope = _fixture.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorage>();
        var attachment = await storage.Upload(
            new UploadRequest
            {
                Document = new Document
                {
                    DocumentId = documentId,
                    MeldingId = meldingId,
                    FileMetadata = new FileMetadata
                    {
                        FileName = "detachable.txt",
                        ContentType = "text/plain",
                    },
                    ScanResult = DocumentScanResult.Clean,
                },
                InputStream = new MemoryStream("preserved blob"u8.ToArray()),
            },
            TestContext.Current.CancellationToken
        );
        var mainContent = await storage.Upload(
            new UploadRequest
            {
                Document = new Document
                {
                    DocumentId = mainDocumentId,
                    MeldingId = meldingId,
                    FileMetadata = new FileMetadata
                    {
                        FileName = "main.pdf",
                        ContentType = "application/pdf",
                    },
                    ScanResult = DocumentScanResult.Clean,
                },
                InputStream = new MemoryStream("main"u8.ToArray()),
            },
            TestContext.Current.CancellationToken
        );
        await scope
            .ServiceProvider.GetRequiredService<IMeldingRepository>()
            .CreateMelding(
                new Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto.CreateMeldingRequest
                {
                    Id = meldingId,
                    Source = MessageSource.Api,
                    ApplicationId = "ulykkesvarsel",
                    ReceivedAt = DateTime.UtcNow,
                    MainDocumentData = mainIsStructuredData ? null : mainContent.PersistedDocument,
                    StructuredData = mainIsStructuredData ? mainContent.PersistedDocument : null,
                    AttachmentData = [attachment.PersistedDocument],
                },
                TestContext.Current.CancellationToken
            );
        return (meldingId, documentId, mainDocumentId);
    }

    [Fact]
    public async Task Subscription_Lifecycle_Works()
    {
        // Use an app id that is NOT part of the fixture seeding, so this test
        // can verify register -> deregister -> NotFound without interference.
        var appToDeregisterAndReregister = "delete-subscription-test-app";

        // Act
        var subscriptionResponse = await _client.PostAsJsonAsync(
            $"/subscriptions",
            new ConsumerManifest
            {
                ConsumerName = "delete-subscription-test-consumer",
                AppRegistrations =
                [
                    new AppRegistration
                    {
                        AppId = appToDeregisterAndReregister,
                        MessageSource = MessageSource.Altinn,
                    },
                ],
            },
            TestContext.Current.CancellationToken
        );

        subscriptionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var activeSubscription = await _client.GetFromJsonAsync<AltinnEventsSubscription>(
            $"/altinn/subscriptions/{appToDeregisterAndReregister}",
            _jsonSerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );

        activeSubscription?.Id.ShouldBeGreaterThan(0);

        var subscriptionResponse2 = await _client.PostAsJsonAsync(
            $"/subscriptions",
            new ConsumerManifest
            {
                ConsumerName = "delete-subscription-test-consumer",
                AppRegistrations = [],
            },
            TestContext.Current.CancellationToken
        );

        subscriptionResponse2.StatusCode.ShouldBe(HttpStatusCode.OK);

        var notFoundResponse = await _client.GetAsync(
            $"/altinn/subscriptions/{appToDeregisterAndReregister}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        notFoundResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WebhookController_ValidateSubscription_ReturnsOk()
    {
        // Arrange
        var cloudEvent = TestData.CloudEvent() with
        {
            Type = "platform.events.validatesubscription",
            DataContentType = new ContentType(MediaTypeNames.Application.Json)
            {
                Name = "application/json",
            },
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/webhook/receive-altinn-cloudevent",
            cloudEvent,
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WebhookController_ReceiveMelding_ReturnsMeldingId()
    {
        // Arrange
        var cloudEvent = TestData.CloudEvent() with
        {
            DataContentType = new ContentType(MediaTypeNames.Application.Json)
            {
                Name = "application/json",
            },
        };

        var subscriptionResponse = await _client.PostAsJsonAsync(
            $"/subscriptions",
            new ConsumerManifest
            {
                ConsumerName = "altinn-test-app",
                AppRegistrations =
                [
                    new AppRegistration { AppId = "ipsam", MessageSource = MessageSource.Altinn },
                ],
            },
            TestContext.Current.CancellationToken
        );

        subscriptionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await _client.PostAsJsonAsync(
            "/webhook/receive-altinn-cloudevent",
            cloudEvent,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var postMeldingResponse = await response.Content.ReadFromJsonAsync<PostMeldingResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        postMeldingResponse?.MeldingId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetMeldingByShortId_MultipleMatches_ReturnsConflictWithMatchingIds()
    {
        // Arrange: two meldinger whose GUIDs share the same trailing 12 hex chars (short id).
        const string sharedShortId = "abcabcabcabc";
        var firstMeldingId = Guid.Parse($"11111111-1111-1111-1111-{sharedShortId}");
        var secondMeldingId = Guid.Parse($"22222222-2222-2222-2222-{sharedShortId}");

        await PostMeldingWithId(firstMeldingId);
        await PostMeldingWithId(secondMeldingId);

        // Act
        var response = await _client.GetAsync(
            $"/meldinger/by-short-id/{sharedShortId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var conflict =
            await response.Content.ReadFromJsonAsync<GetMeldingByShortIdConflictResponse>(
                _jsonSerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

        conflict.ShouldNotBeNull();
        conflict.MatchingMeldingIds.ShouldContain(firstMeldingId);
        conflict.MatchingMeldingIds.ShouldContain(secondMeldingId);
    }

    private async Task PostMeldingWithId(Guid meldingId)
    {
        var body = CreatePostMeldingBody() with
        {
            MeldingId = meldingId,
            MainContent = TestData.CreateFormFile(
                "mainContent.txt",
                "Hello World",
                contentType: "text/plain"
            ),
        };

        var response = await _client.PostAsync(
            "/meldinger",
            body.ToMultipartFormDataContent(),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static PostMeldingBody CreatePostMeldingBody()
    {
        return TestData.CreatePostMeldingBodyFaker().Generate() with
        {
            ApplicationId = ApplicationFixture.KnownApplicationId,
        };
    }
}
