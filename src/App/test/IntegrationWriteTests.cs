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
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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
        (
            await mainDocumentResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken
            )
        ).ShouldContain("Main content and structured data cannot be deleted.");

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
        applied.Revision.ShouldBe(freshPreview.Revision);

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
            .GetDocumentAsync(
                documentId,
                TestContext.Current.CancellationToken,
                includeDeleted: true
            );
        document.ShouldNotBeNull();
        document.IsDeleted.ShouldBeTrue();
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

        var listedDocuments = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents",
            _jsonSerializerOptions,
            cancellationToken: TestContext.Current.CancellationToken
        );
        listedDocuments
            .ShouldNotBeNull()
            .Documents.Select(document => document.DocumentId)
            .ShouldBe([mainDocumentId]);

        var documentRoute = $"/meldinger/{meldingId}/documents/{documentId}";
        using var hiddenLookup = await _client.GetAsync(
            documentRoute,
            TestContext.Current.CancellationToken
        );
        hiddenLookup.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var hiddenDownload = await _client.GetAsync(
            $"{documentRoute}/download",
            TestContext.Current.CancellationToken
        );
        hiddenDownload.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var downloadWithIgnoredQuery = await _client.GetAsync(
            $"{documentRoute}/download?includeDeleted=true",
            TestContext.Current.CancellationToken
        );
        downloadWithIgnoredQuery.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var deletedDocument = await _client.GetFromJsonAsync<Document>(
            $"{documentRoute}?includeDeleted=true",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        deletedDocument.ShouldNotBeNull().IsDeleted.ShouldBeTrue();
        using var foreignLookup = await _client.GetAsync(
            $"/meldinger/{unrelatedMelding.MeldingId}/documents/{documentId}?includeDeleted=true",
            TestContext.Current.CancellationToken
        );
        foreignLookup.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var allDocuments = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents?includeDeleted=true",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        allDocuments.ShouldNotBeNull().Documents.Count.ShouldBe(2);
        allDocuments.Documents.Single(d => d.DocumentId == documentId).IsDeleted.ShouldBeTrue();

        var shortId = meldingId.ToString("D").Split('-')[^1];
        foreach (
            var meldingRoute in new[]
            {
                $"/meldinger/{meldingId}",
                $"/meldinger/by-short-id/{shortId}",
            }
        )
        {
            var hidden = await _client.GetFromJsonAsync<GetMeldingResponse>(
                meldingRoute,
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            );
            hidden.ShouldNotBeNull().Melding.ShouldNotBeNull().AttachmentIds.ShouldBeEmpty();
            var included = await _client.GetFromJsonAsync<GetMeldingResponse>(
                $"{meldingRoute}?includeDeleted=true",
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            );
            included
                .ShouldNotBeNull()
                .Melding.ShouldNotBeNull()
                .AttachmentIds.ShouldBe([documentId]);
        }
        foreach (var includeDeleted in new[] { false, true })
        {
            var page = await _client.GetFromJsonAsync<GetAllMeldingerResponse>(
                $"/meldinger?pageSize=100&includeDeleted={includeDeleted}",
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            );
            page.ShouldNotBeNull()
                .Items.Single(m => m.Id == meldingId)
                .AttachmentIds.ShouldBe(includeDeleted ? [documentId] : []);
        }

        var restoreRoute = $"/meldinger/{meldingId}/attachment-restorations";
        var restorePreview = await PreviewAttachmentChange(restoreRoute, documentId);
        restorePreview.AlreadyDetached.ShouldBeTrue();
        restorePreview.AttachmentIdsBefore.ShouldBeEmpty();
        restorePreview.AttachmentIdsAfter.ShouldBe([documentId]);
        using var stillHidden = await _client.GetAsync(
            documentRoute,
            TestContext.Current.CancellationToken
        );
        stillHidden.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var staleRestore = await _client.PostAsJsonAsync(
            restoreRoute,
            new AttachmentDetachmentRequest
            {
                DocumentId = documentId,
                Apply = true,
                ExpectedRevision = preview.Revision,
            },
            TestContext.Current.CancellationToken
        );
        staleRestore.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var restored = await ApplyAttachmentChange(
            restoreRoute,
            documentId,
            restorePreview.Revision
        );
        restored.Applied.ShouldBeTrue();
        restored.AlreadyDetached.ShouldBeTrue();
        restored.Revision.ShouldNotBe(restorePreview.Revision);
        var restoredPreview = await PreviewAttachmentChange(restoreRoute, documentId);
        restoredPreview.Revision.ShouldBe(restored.Revision);
        restoredPreview.AlreadyDetached.ShouldBeFalse();
        var noOp = await ApplyAttachmentChange(restoreRoute, documentId, restored.Revision);
        noOp.Revision.ShouldBe(restored.Revision);

        var activeDocument = await _client.GetFromJsonAsync<Document>(
            documentRoute,
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        activeDocument.ShouldNotBeNull().IsDeleted.ShouldBeFalse();
        using var restoredDownload = await _client.GetAsync(
            $"{documentRoute}/download",
            TestContext.Current.CancellationToken
        );
        restoredDownload.StatusCode.ShouldBe(HttpStatusCode.OK);
        (
            await restoredDownload.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        ).ShouldBe("preserved blob");
        var restoredDocuments = await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
            $"/meldinger/{meldingId}/documents",
            _jsonSerializerOptions,
            TestContext.Current.CancellationToken
        );
        restoredDocuments.ShouldNotBeNull().Documents.Count.ShouldBe(2);

        var deleteAgainPreview = await PreviewAttachmentChange(route, documentId);
        await ApplyAttachmentChange(route, documentId, deleteAgainPreview.Revision);
        using var deletedAgain = await _client.GetAsync(
            documentRoute,
            TestContext.Current.CancellationToken
        );
        deletedAgain.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AttachmentDeletion_RejectsMainAndStructuredDocuments_ButRestorationIsNoOp(
        bool structuredData
    )
    {
        var (meldingId, _, protectedId, _) = await CreateAltinnMeldingWithAttachment(
            mainIsStructuredData: structuredData
        );
        using var deletion = await _client.PostAsJsonAsync(
            $"/meldinger/{meldingId}/attachment-detachments",
            new AttachmentDetachmentRequest { DocumentId = protectedId },
            TestContext.Current.CancellationToken
        );
        deletion.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (
            await deletion.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        ).ShouldContain("Main content and structured data cannot be deleted.");
        var route = $"/meldinger/{meldingId}/attachment-restorations";
        var preview = await PreviewAttachmentChange(route, protectedId);
        var applied = await ApplyAttachmentChange(route, protectedId, preview.Revision);
        applied.AlreadyDetached.ShouldBeFalse();
        applied.Revision.ShouldBe(preview.Revision);
        applied.AttachmentIdsAfter.ShouldBe(preview.AttachmentIdsBefore);
    }

    [Theory]
    [InlineData(MessageSource.Api)]
    [InlineData(MessageSource.Altinn)]
    public async Task AttachmentDeletion_AnyApplicationAndSource_CanDeleteAndRestore(
        MessageSource source
    )
    {
        var (meldingId, documentId, _, _) = await CreateAltinnMeldingWithAttachment(
            "another-app",
            source
        );
        var route = $"/meldinger/{meldingId}/attachment-detachments";
        var preview = await PreviewAttachmentChange(route, documentId);
        await ApplyAttachmentChange(route, documentId, preview.Revision);
        var restoreRoute = $"/meldinger/{meldingId}/attachment-restorations";
        var restorePreview = await PreviewAttachmentChange(restoreRoute, documentId);
        await ApplyAttachmentChange(restoreRoute, documentId, restorePreview.Revision);
        using var lookup = await _client.GetAsync(
            $"/meldinger/{meldingId}/documents/{documentId}",
            TestContext.Current.CancellationToken
        );
        lookup.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("attachment-detachments")]
    [InlineData("attachment-restorations")]
    public async Task AttachmentDeletionChange_RejectsInvalidRequests(string operation)
    {
        var (meldingId, documentId, _, _) = await CreateAltinnMeldingWithAttachment();
        var route = $"/meldinger/{meldingId}/{operation}";
        using var missingRevision = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = documentId, Apply = true },
            TestContext.Current.CancellationToken
        );
        missingRevision.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var emptyDocument = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = Guid.Empty },
            TestContext.Current.CancellationToken
        );
        emptyDocument.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var unknownDocument = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = Guid.NewGuid() },
            TestContext.Current.CancellationToken
        );
        unknownDocument.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var missingMelding = await _client.PostAsJsonAsync(
            $"/meldinger/{Guid.NewGuid()}/{operation}",
            new AttachmentDetachmentRequest { DocumentId = documentId },
            TestContext.Current.CancellationToken
        );
        missingMelding.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var foreignMelding = await CreateAltinnMeldingWithAttachment();
        using var foreignDocument = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = foreignMelding.DocumentId },
            TestContext.Current.CancellationToken
        );
        foreignDocument.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AttachmentDeletionChange_ConcurrentApplies_OnlyOneSucceeds(bool restore)
    {
        var (meldingId, documentId, _, _) = await CreateAltinnMeldingWithAttachment();
        var deletionRoute = $"/meldinger/{meldingId}/attachment-detachments";
        if (restore)
        {
            var deletionPreview = await PreviewAttachmentChange(deletionRoute, documentId);
            await ApplyAttachmentChange(deletionRoute, documentId, deletionPreview.Revision);
        }

        var route = restore ? $"/meldinger/{meldingId}/attachment-restorations" : deletionRoute;
        var preview = await PreviewAttachmentChange(route, documentId);
        preview.AlreadyDetached.ShouldBe(restore);
        var request = new AttachmentDetachmentRequest
        {
            DocumentId = documentId,
            Apply = true,
            ExpectedRevision = preview.Revision,
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var cancellationToken = timeout.Token;

        using var scope = _fixture.Services.CreateScope();
        var connectionString = scope
            .ServiceProvider.GetRequiredService<ReceiverDbContext>()
            .Database.GetConnectionString();
        await using var blocker = new NpgsqlConnection(connectionString);
        await blocker.OpenAsync(cancellationToken);
        await using var transaction = await blocker.BeginTransactionAsync(cancellationToken);
        await using var lockCommand = new NpgsqlCommand(
            """SELECT "Id" FROM public.meldinger WHERE "Id" = @meldingId FOR UPDATE""",
            blocker,
            transaction
        );
        lockCommand.Parameters.AddWithValue("meldingId", meldingId);
        (await lockCommand.ExecuteScalarAsync(cancellationToken)).ShouldBe(meldingId);

        // Separate HTTP requests resolve separate scoped DbContexts. Hold both at the same row lock.
        var firstApply = _client.PostAsJsonAsync(route, request, cancellationToken);
        var secondApply = _client.PostAsJsonAsync(route, request, cancellationToken);
        try
        {
            await using var observer = new NpgsqlConnection(connectionString);
            await observer.OpenAsync(cancellationToken);
            await using var blockedRequests = new NpgsqlCommand(
                """
                WITH RECURSIVE blocked_requests(pid) AS (
                    SELECT pid FROM pg_stat_activity
                    WHERE @blockerPid = ANY(pg_blocking_pids(pid))
                    UNION
                    SELECT activity.pid FROM pg_stat_activity activity
                    JOIN blocked_requests blocked ON blocked.pid = ANY(pg_blocking_pids(activity.pid))
                )
                SELECT count(DISTINCT activity.pid)
                FROM pg_stat_activity activity
                JOIN blocked_requests blocked ON blocked.pid = activity.pid
                WHERE activity.wait_event_type = 'Lock'
                    AND activity.query LIKE '%public.meldinger%'
                """,
                observer
            );
            blockedRequests.Parameters.AddWithValue("blockerPid", blocker.ProcessID);
            while ((long)(await blockedRequests.ExecuteScalarAsync(cancellationToken))! != 2)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
            }
            firstApply.IsCompleted.ShouldBeFalse();
            secondApply.IsCompleted.ShouldBeFalse();
        }
        finally
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }

        var responses = await Task.WhenAll(firstApply, secondApply);
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        var successfulResponse = responses.Single(r => r.StatusCode == HttpStatusCode.OK);
        var applied = (
            await successfulResponse.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                cancellationToken
            )
        ).ShouldNotBeNull();
        applied.Applied.ShouldBeTrue();
        applied.AlreadyDetached.ShouldBe(restore);
        applied.Revision.ShouldNotBe(preview.Revision);
        applied.AttachmentIdsAfter.ShouldBe(restore ? [documentId] : []);

        using var verificationScope = _fixture.Services.CreateScope();
        var persistedDocument = await verificationScope
            .ServiceProvider.GetRequiredService<IDocumentRepository>()
            .GetDocumentAsync(documentId, cancellationToken, includeDeleted: true);
        persistedDocument.ShouldNotBeNull().IsDeleted.ShouldBe(!restore);
        var persistedMelding = await verificationScope
            .ServiceProvider.GetRequiredService<IMeldingRepository>()
            .GetMelding(meldingId, cancellationToken);
        persistedMelding.ShouldNotBeNull().AttachmentIds.ShouldBe(applied.AttachmentIdsAfter);
        var currentPreview = await PreviewAttachmentChange(route, documentId);
        currentPreview.Revision.ShouldBe(applied.Revision);
    }

    private async Task<AttachmentDetachmentResponse> PreviewAttachmentChange(
        string route,
        Guid documentId
    )
    {
        using var response = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest { DocumentId = documentId },
            TestContext.Current.CancellationToken
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (
            await response.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            )
        ).ShouldNotBeNull();
    }

    private async Task<AttachmentDetachmentResponse> ApplyAttachmentChange(
        string route,
        Guid documentId,
        string revision
    )
    {
        using var response = await _client.PostAsJsonAsync(
            route,
            new AttachmentDetachmentRequest
            {
                DocumentId = documentId,
                Apply = true,
                ExpectedRevision = revision,
            },
            TestContext.Current.CancellationToken
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (
            await response.Content.ReadFromJsonAsync<AttachmentDetachmentResponse>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            )
        ).ShouldNotBeNull();
    }

    private async Task<(
        Guid MeldingId,
        Guid DocumentId,
        Guid MainDocumentId,
        string StorageReference
    )> CreateAltinnMeldingWithAttachment(
        string applicationId = "ulykkesvarsel",
        MessageSource source = MessageSource.Altinn,
        bool mainIsStructuredData = false
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
                    MainDocumentData = mainIsStructuredData ? null : mainContent.PersistedDocument,
                    StructuredData = mainIsStructuredData ? mainContent.PersistedDocument : null,
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
