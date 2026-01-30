using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
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
    public async Task PostMelding_ThenGetMelding_ReturnsMelding()
    {
        var postMeldingBody = TestData.CreatePostMeldingBodyFaker().Generate() with
        {
            ApplicationId = ApplicationFixture.KnownApplicationId,
            Metadata = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
            },
            MainContent = CreateFormFile("mainContent.txt", "Hello World"),
            Attachments =
            [
                CreateFormFile("attachment1.txt", "Attachment 1 content"),
                CreateFormFile("attachment2.txt", "Attachment 2 content"),
            ],
        };

        var postMeldingResponse = await (
            await _client.PostAsync(
                "/meldinger",
                postMeldingBody.ToMultipartFormDataContent(),
                TestContext.Current.CancellationToken
            )
        ).Content.ReadFromJsonAsync<PostMeldingResponse>(
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

    private static IFormFile CreateFormFile(string name, string content)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new FormFile(stream, 0, content.Length, name, name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain",
        };
    }
}

file static class Extensions
{
    public static MultipartFormDataContent ToMultipartFormDataContent(this PostMeldingBody body)
    {
        var content = new MultipartFormDataContent();

        // Add ApplicationId
        content.Add(new StringContent(body.ApplicationId), nameof(PostMeldingBody.ApplicationId));

        // Add Metadata
        foreach (var kvp in body.Metadata)
            content.Add(
                new StringContent(kvp.Value),
                $"{nameof(PostMeldingBody.Metadata)}[{kvp.Key}]"
            );

        // Add MainContent
        var mainContentStream = new MemoryStream();
        body.MainContent.CopyTo(mainContentStream);
        mainContentStream.Position = 0; // Reset stream position

        var mainContent = new StreamContent(mainContentStream);
        mainContent.Headers.ContentType = new MediaTypeHeaderValue(
            body.MainContent.ContentType ?? "application/octet-stream"
        );
        content.Add(mainContent, nameof(PostMeldingBody.MainContent), body.MainContent.FileName);

        // Add Attachments
        foreach (var attachment in body.Attachments)
        {
            var attachmentStream = new MemoryStream();
            attachment.CopyTo(attachmentStream);
            attachmentStream.Position = 0; // Reset stream position

            var attachmentContent = new StreamContent(attachmentStream);
            attachmentContent.Headers.ContentType = new MediaTypeHeaderValue(
                attachment.ContentType ?? "application/octet-stream"
            );
            content.Add(
                attachmentContent,
                nameof(PostMeldingBody.Attachments),
                attachment.FileName
            );
        }

        return content;
    }
}
