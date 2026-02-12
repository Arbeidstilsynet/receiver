using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test.fixture;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Arbeidstilsynet.Receiver.Model.Request;
using Arbeidstilsynet.Receiver.Model.Response;
using Argon;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test;

public class IntegrationWriteTests : IClassFixture<ApplicationFixture>
{
    private readonly HttpClient _client;
    private readonly VerifySettings _verifySettings = new();

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public IntegrationWriteTests(ApplicationFixture factory)
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
        var cloudEvent = TestData.CloudEvent(e =>
        {
            e.Type = "platform.events.validatesubscription";
            e.DataContentType = new ContentType(MediaTypeNames.Application.Json)
            {
                Name = "application/json",
            };
        });

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
        var cloudEvent = TestData.CloudEvent(e =>
        {
            e.DataContentType = new ContentType(MediaTypeNames.Application.Json)
            {
                Name = "application/json",
            };
        });
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

    
    private static PostMeldingBody CreatePostMeldingBody()
    {
        return TestData.CreatePostMeldingBodyFaker().Generate() with
        {
            ApplicationId = ApplicationFixture.KnownApplicationId,
        };
    }
}
