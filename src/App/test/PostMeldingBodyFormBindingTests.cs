using System.Net.Http.Json;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test;

/// <summary>
/// Pins the multipart contract of POST /meldinger without Docker: MVC binds
/// <see cref="PostMeldingBody.Metadata"/> from bracketed form keys only. The npm SDK's
/// <c>receiverFormDataBodySerializer</c> emits this shape; hey-api's default form serializer
/// sends <c>Metadata</c> as one JSON field, which binds to an empty dictionary.
/// </summary>
public class PostMeldingBodyFormBindingTests
{
    [Fact]
    public async Task Bind_WhenMetadataUsesBracketedKeys_BindsDictionary()
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent("ulykkesvarsel"), "ApplicationId" },
            { new StringContent("123456789"), "Metadata[orgnr]" },
            { new StringContent("Oslo"), "Metadata[kommune]" },
        };

        var metadata = await PostAndReadMetadata(content);

        metadata.ShouldBe(
            new Dictionary<string, string> { ["orgnr"] = "123456789", ["kommune"] = "Oslo" },
            ignoreOrder: true
        );
    }

    [Fact]
    public async Task Bind_WhenMetadataIsSingleJsonField_LosesMetadata()
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent("ulykkesvarsel"), "ApplicationId" },
            { new StringContent("""{"orgnr":"123456789"}"""), "Metadata" },
        };

        var metadata = await PostAndReadMetadata(content);

        metadata.ShouldBeEmpty();
    }

    private static async Task<Dictionary<string, string>> PostAndReadMetadata(
        MultipartFormDataContent content
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder
            .Services.AddControllers()
            .AddApplicationPart(typeof(FormBindingProbeController).Assembly);
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var response = await app.GetTestClient()
            .PostAsync("/form-binding-probe", content, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        return (
            await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(
                TestContext.Current.CancellationToken
            )
        )!;
    }
}

[ApiController]
[Route("form-binding-probe")]
public class FormBindingProbeController : ControllerBase
{
    // Same binding attributes as MeldingerController.PostMelding.
    [HttpPost]
    [Consumes("multipart/form-data")]
    public ActionResult<Dictionary<string, string>> Post([FromForm] PostMeldingBody model) =>
        model.Metadata;
}
