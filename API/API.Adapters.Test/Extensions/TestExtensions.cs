using System.Net.Http.Headers;
using Arbeidstilsynet.Receiver.Model.Request;
using Microsoft.AspNetCore.Http;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test.Extensions;

internal static class TestExtensions
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
        if (body.MainContent is { } mainContentFile)
        {
            var mainContent = mainContentFile.ToStreamContent();
            content.Add(
                mainContent,
                nameof(PostMeldingBody.MainContent),
                body.MainContent.FileName
            );
        }

        // Add StructuredData
        if (body.StructuredData is { } structuredDataFile)
        {
            var structuredDataContent = structuredDataFile.ToStreamContent();
            content.Add(
                structuredDataContent,
                nameof(PostMeldingBody.StructuredData),
                body.StructuredData.FileName
            );
        }

        // Add Attachments
        foreach (var attachment in body.Attachments)
        {
            var attachmentContent = attachment.ToStreamContent();
            content.Add(
                attachmentContent,
                nameof(PostMeldingBody.Attachments),
                attachment.FileName
            );
        }

        return content;
    }

    private static StreamContent ToStreamContent(this IFormFile file)
    {
        var stream = new MemoryStream();
        file.CopyTo(stream);
        stream.Position = 0; // Reset stream position

        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            file.ContentType ?? "application/octet-stream"
        );
        return content;
    }
}