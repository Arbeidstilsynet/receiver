using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test.fixture;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using NSubstitute;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test;

public class AdHocControllerTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private const string MainContentFileName = "mainContent.txt";

    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly List<PostMeldingBody> _seededMeldinger;

    private readonly IMeldingNotificationService _meldingNotificationService;

    public AdHocControllerTests(ApplicationFixture factory)
    {
        _client = factory.CreateClient();
        _seededMeldinger = TestData
            .CreatePostMeldingBodyFaker()
            .Generate(3)
            .Select(m =>
                (
                    m with
                    {
                        ApplicationId = ApplicationFixture.AdHocApplicationId,
                        StructuredData = TestData.CreateFormFile(
                            "structuredData.json",
                            "{ \"key\": \"value\" }",
                            "application/json"
                        ),
                        Attachments =
                        [
                            TestData.CreateFormFile(
                                MainContentFileName,
                                "This should be the main document",
                                "text/plain"
                            ),
                            TestData.CreateFormFile(
                                "some-attachment.txt",
                                "Attachment content",
                                "text/plain"
                            ),
                        ],
                    }
                )
            )
            .ToList();

        _meldingNotificationService = factory.NotificationServiceMock;
    }

    // Initialize test

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        foreach (var m in _seededMeldinger)
        {
            var response = await _client.PostAsync(
                "/meldinger",
                m.ToMultipartFormDataContent(),
                cancellationToken: TestContext.Current.CancellationToken
            );

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task MigrateDocuments_FindMainDocument()
    {
        _meldingNotificationService.ClearReceivedCalls();

        var myAppMeldinger = (
            await _client.GetFromJsonAsync<GetAllMeldingerResponse>(
                "meldinger?pageSize=100",
                _jsonSerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            )
        )
            ?.Items.Where(m => m.ApplicationId == ApplicationFixture.AdHocApplicationId)
            .ToList()!;

        myAppMeldinger.Count.ShouldBeGreaterThan(0);

        // Act
        foreach (var m in myAppMeldinger)
        {
            var documents = (
                await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
                    $"/meldinger/{m.Id}/documents",
                    _jsonSerializerOptions,
                    TestContext.Current.CancellationToken
                )
            )?.Documents!;

            var newMainContent = documents
                .First(d => d.FileMetadata.FileName == MainContentFileName)
                .DocumentId;
            var expectedRemainingAttachments = documents
                .Where(d => d.DocumentId != newMainContent && d.DocumentId != m.StructuredDataId)
                .Select(d => d.DocumentId)
                .ToList();

            var newAttachments = expectedRemainingAttachments.Take(2).ToList();

            var httpResponse = await _client.PostAsJsonAsync(
                "/adHoc/editMelding",
                new PostEditMeldingBody() { MeldingId = m.Id, NewMainContentId = newMainContent },
                _jsonSerializerOptions,
                cancellationToken: TestContext.Current.CancellationToken
            );

            httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            var resultMelding = await httpResponse.Content.ReadFromJsonAsync<Melding>(
                _jsonSerializerOptions,
                TestContext.Current.CancellationToken
            );

            resultMelding.ShouldNotBeNull();
            resultMelding.Id.ShouldBeEquivalentTo(m.Id);
            resultMelding.MainContentId.ShouldBe(newMainContent);
            resultMelding.StructuredDataId.ShouldBe(m.StructuredDataId);
            resultMelding.AttachmentIds.ShouldBeEquivalentTo(expectedRemainingAttachments);

            var docs = (
                await _client.GetFromJsonAsync<GetAllDocumentsResponse>(
                    $"/meldinger/{m.Id}/documents",
                    _jsonSerializerOptions,
                    TestContext.Current.CancellationToken
                )
            )?.Documents!;

            docs.Count.ShouldBe(2 + newAttachments.Count);
            await _meldingNotificationService
                .Received(1)
                .NotifyMeldingProcessed(Arg.Is<Melding>(p => p.Id == resultMelding.Id));
        }
    }
}
