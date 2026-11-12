using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Arbeidstilsynet.MeldingerReceiver.App.Extensions;
using Arbeidstilsynet.MeldingerReceiver.App.WebApi;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test;

/// <summary>
/// Pins the multipart contract of POST /meldinger without Docker, using the production MVC
/// configuration (<see cref="StartupExtensions.ConfigureApi"/>) and the real
/// <c>MeldingerController</c> with a mocked <see cref="IMeldingService"/>. Metadata is accepted
/// either as bracketed form keys (<c>Metadata[key]</c>) or as one JSON form field
/// (<c>Metadata</c>), which is what the generated hey-api SDK sends.
/// </summary>
public class PostMeldingBodyFormBindingTests
{
    private const string FixtureBoundary = "receiver-fixture-boundary";

    private readonly IMeldingService _meldingService = Substitute.For<IMeldingService>();
    private CreateMeldingRequest? _received;

    public PostMeldingBodyFormBindingTests()
    {
        _meldingService
            .ProcessMelding(Arg.Any<CreateMeldingRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<CreateMeldingRequest>();
                _received = request;
                return new Melding
                {
                    Id = request.MeldingId,
                    Source = request.Source,
                    ApplicationId = request.ApplicationReference,
                    ReceivedAt = DateTime.UtcNow,
                };
            });
    }

    [Fact]
    public async Task PostMelding_WhenMetadataUsesBracketedKeys_BindsMetadata()
    {
        using var content = CreateContent(
            ("Metadata[orgnr]", "123456789"),
            ("Metadata[kommune]", "Oslo")
        );

        var response = await Post(content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _received!.Metadata.ShouldBe(ExpectedMetadata, ignoreOrder: true);
    }

    [Fact]
    public async Task PostMelding_WhenMetadataIsJsonField_BindsMetadata()
    {
        using var content = CreateContent(
            ("Metadata", """{"orgnr":"123456789","kommune":"Oslo"}""")
        );

        var response = await Post(content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _received!.Metadata.ShouldBe(ExpectedMetadata, ignoreOrder: true);
    }

    [Fact]
    public async Task PostMelding_WhenMetadataIsAbsent_BindsEmptyMetadata()
    {
        using var content = CreateContent();

        var response = await Post(content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _received!.Metadata.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""["orgnr"]""")]
    [InlineData("""{"orgnr":123456789}""")]
    [InlineData("""{"orgnr":null}""")]
    [InlineData("null")]
    [InlineData("")]
    public async Task PostMelding_WhenMetadataJsonIsInvalid_ReturnsValidationProblem(string json)
    {
        using var content = CreateContent(("Metadata", json));

        var response = await Post(content);

        await ShouldBeMetadataValidationProblem(response);
    }

    [Fact]
    public async Task PostMelding_WhenMetadataIsSentAsJsonAndBracketedKeys_ReturnsValidationProblem()
    {
        using var content = CreateContent(
            ("Metadata", """{"orgnr":"123456789"}"""),
            ("Metadata[kommune]", "Oslo")
        );

        var response = await Post(content);

        await ShouldBeMetadataValidationProblem(response);
    }

    [Fact]
    public async Task PostMelding_WhenBodyIsCapturedFromGeneratedSdk_RoundTripsMetadataAndFiles()
    {
        // Written by `pnpm build:npm && pnpm test:npm` from the built SDK's default postMeldinger.
        var body = await File.ReadAllTextAsync(
            GetSdkFixturePath(),
            TestContext.Current.CancellationToken
        );
        using var content = new ByteArrayContent(
            Encoding.UTF8.GetBytes(body.ReplaceLineEndings("\r\n"))
        );
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            $"multipart/form-data; boundary={FixtureBoundary}"
        );

        var response = await Post(content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _received!.ApplicationReference.ShouldBe("ulykkesvarsel");
        _received.Metadata.ShouldBe(ExpectedMetadata, ignoreOrder: true);
        _received.MainContent!.FileMetadata.FileName.ShouldBe("melding.pdf");
        _received.Attachments.Select(a => a.FileMetadata.FileName).ShouldBe(["a.txt", "b.txt"]);
    }

    private static Dictionary<string, string> ExpectedMetadata =>
        new() { ["orgnr"] = "123456789", ["kommune"] = "Oslo" };

    private static string GetSdkFixturePath([CallerFilePath] string testFile = "") =>
        Path.Combine(Path.GetDirectoryName(testFile)!, "fixture", "post-meldinger.multipart");

    private static MultipartFormDataContent CreateContent(
        params (string Name, string Value)[] fields
    )
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent("ulykkesvarsel"), "ApplicationId" },
        };
        var mainContent = new ByteArrayContent("%PDF"u8.ToArray());
        mainContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(mainContent, "MainContent", "melding.pdf");
        foreach (var (name, value) in fields)
        {
            content.Add(new StringContent(value), name);
        }
        return content;
    }

    private async Task<HttpResponseMessage> Post(HttpContent content)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureApi().AddApplicationPart(typeof(IAssemblyInfo).Assembly);
        builder.Services.AddMetrics();
        builder.Services.AddSingleton<ApiMeters>();
        builder.Services.AddSingleton(_meldingService);
        builder.Services.AddValidatorsFromAssemblyContaining<IAssemblyInfo>(
            includeInternalTypes: true
        );
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var response = await app.GetTestClient()
            .PostAsync("/meldinger", content, TestContext.Current.CancellationToken);
        await response.Content.LoadIntoBufferAsync(TestContext.Current.CancellationToken);
        return response;
    }

    private async Task ShouldBeMetadataValidationProblem(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        problem.ShouldContain("\"Metadata\"");
        _received.ShouldBeNull();
    }
}
