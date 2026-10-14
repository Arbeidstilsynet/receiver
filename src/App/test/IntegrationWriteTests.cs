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
    public async Task DetachAttachment_PreviewApplyAndRetry_PreservesDocumentAndRemovesOnlyAttachment()
    {
        var (meldingId, documentId, mainDocumentId, storageReference) =
            await CreateAltinnMeldingWithAttachment();
        var route = $"/meldinger/{meldingId}/attachment-detachments";
        var previewResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = documentId },
            TestContext.Current.CancellationToken
        );

        previewResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var preview = (
            await previewResponse.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            )
        ).ShouldNotBeNull();
        preview.MeldingId.ShouldBe(meldingId);
        preview.DocumentId.ShouldBe(documentId);
        preview.ApplicationId.ShouldBe("ulykkesvarsel");
        preview.MainContentId.ShouldBe(mainDocumentId);
        preview.StructuredDataId.ShouldBeNull();
        preview.AttachmentIdsBefore.ShouldBe([documentId]);
        preview.AttachmentIdsAfter.ShouldBe([]);
        preview.FileName.ShouldBe("detachable.txt");
        preview.ContentType.ShouldBe("text/plain");
        preview.Applied.ShouldBeFalse();
        preview.AlreadyDetached.ShouldBeFalse();

        var stillAttached = await _client.GetFromJsonAsync<GetMeldingResponse>(
            $"/meldinger/{meldingId}",
            _jsonSerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );
        stillAttached
            .ShouldNotBeNull()
            .Melding.ShouldNotBeNull()
            .AttachmentIds.ShouldContain(documentId);

        var mainDocumentResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = mainDocumentId },
            TestContext.Current.CancellationToken
        );
        mainDocumentResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var unknownDocumentResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = Guid.NewGuid() },
            TestContext.Current.CancellationToken
        );
        unknownDocumentResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var unrelatedMelding = await CreateAltinnMeldingWithAttachment();
        var foreignDocumentResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = unrelatedMelding.DocumentId },
            TestContext.Current.CancellationToken
        );
        foreignDocumentResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var unsupportedApplication = await CreateAltinnMeldingWithAttachment("another-app");
        var unsupportedApplicationResponse = await _client.PostAsJsonAsync(
            $"/meldinger/{unsupportedApplication.MeldingId}/attachment-detachments",
            new AttachmentDetachmentRequest { DocumentId = unsupportedApplication.DocumentId },
            TestContext.Current.CancellationToken
        );
        unsupportedApplicationResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var unsupportedSource = await CreateAltinnMeldingWithAttachment(source: MessageSource.Api);
        var unsupportedSourceResponse = await _client.PostAsJsonAsync(
            $"/meldinger/{unsupportedSource.MeldingId}/attachment-detachments",
            new AttachmentDetachmentRequest { DocumentId = unsupportedSource.DocumentId },
            TestContext.Current.CancellationToken
        );
        unsupportedSourceResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var emptyDocumentResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = Guid.Empty },
            TestContext.Current.CancellationToken
        );
        emptyDocumentResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var emptyMeldingIdResponse = await _client.PostAsJsonAsync(
            $"/meldinger/{Guid.Empty}/attachment-detachments",
            new AttachmentDetachmentRequest { DocumentId = documentId },
            TestContext.Current.CancellationToken
        );
        emptyMeldingIdResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var applyResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest
            {
                DocumentId = documentId,
                Apply = true,
                ExpectedRevision = preview.Revision,
            },
            TestContext.Current.CancellationToken
        );
        applyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var applied = (
            await applyResponse.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            )
        ).ShouldNotBeNull();
        applied.Applied.ShouldBeTrue();
        applied.AlreadyDetached.ShouldBeFalse();
        applied.AttachmentIdsBefore.ShouldBe([documentId]);
        applied.AttachmentIdsAfter.ShouldBe([]);
        applied.Revision.ShouldNotBe(preview.Revision);

        var freshPreviewResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = documentId },
            TestContext.Current.CancellationToken
        );
        var freshPreview = (
            await freshPreviewResponse.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            )
        ).ShouldNotBeNull();
        freshPreview.AlreadyDetached.ShouldBeTrue();
        freshPreview.AttachmentIdsBefore.ShouldBe([]);
        freshPreview.AttachmentIdsAfter.ShouldBe([]);

        var staleApplyResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest
            {
                DocumentId = documentId,
                Apply = true,
                ExpectedRevision = preview.Revision,
            },
            TestContext.Current.CancellationToken
        );
        staleApplyResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var idempotentApplyResponse = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest
            {
                DocumentId = documentId,
                Apply = true,
                ExpectedRevision = freshPreview.Revision,
            },
            TestContext.Current.CancellationToken
        );
        idempotentApplyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var idempotent = (
            await idempotentApplyResponse.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            )
        ).ShouldNotBeNull();
        idempotent.Applied.ShouldBeTrue();
        idempotent.AlreadyDetached.ShouldBeTrue();

        using var scope = _fixture.Services.CreateScope();
        var document = await scope
            .ServiceProvider.GetRequiredService<IDocumentRepository>()
            .GetDocumentAsync(documentId, TestContext.Current.CancellationToken);
        document.ShouldNotBeNull();
        document.FileMetadata.FileName.ShouldBe("detachable.txt");
        document.FileMetadata.ContentType.ShouldBe("text/plain");

        using var blobContent = new MemoryStream();
        await scope
            .ServiceProvider.GetRequiredService<IDocumentStorage>()
            .Download(storageReference, blobContent, TestContext.Current.CancellationToken);
        System.Text.Encoding.UTF8.GetString(blobContent.ToArray()).ShouldBe("preserved blob");

        var detachedMelding = await _client.GetFromJsonAsync<GetMeldingResponse>(
            $"/meldinger/{meldingId}",
            _jsonSerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );
        detachedMelding.ShouldNotBeNull().Melding.ShouldNotBeNull().AttachmentIds.ShouldBeEmpty();
    }

    private async Task<(
        Guid MeldingId,
        Guid DocumentId,
        Guid MainDocumentId,
        string StorageReference
    )> CreateAltinnMeldingWithAttachment(
        string applicationId = "ulykkesvarsel",
        MessageSource source = MessageSource.Altinn
    )
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
                    Source = source,
                    ApplicationId = applicationId,
                    ReceivedAt = DateTime.UtcNow,
                    MainDocumentData = mainContent.PersistedDocument,
                    AttachmentData = [attachment.PersistedDocument],
                },
                TestContext.Current.CancellationToken
            );
        return (
            meldingId,
            documentId,
            mainDocumentId,
            attachment.PersistedDocument.InternalDocumentReference
        );
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
