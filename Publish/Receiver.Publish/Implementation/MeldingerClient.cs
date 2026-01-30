using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.Receiver.DependencyInjection;
using Arbeidstilsynet.Receiver.Model.Response;
using Arbeidstilsynet.Receiver.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Implementation;

internal class MeldingerClient : IMeldingerClient
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public MeldingerClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(
            DependencyInjectionExtensions.MeldingerReceiverApiClientKey
        );
    }

    public async Task<GetAllDocumentsResponse> GetDocuments(Guid meldingId)
    {
        return await _httpClient.GetFromJsonAsync<GetAllDocumentsResponse>(
                $"meldinger/{meldingId}/documents",
                _jsonSerializerOptions
            ) ?? new GetAllDocumentsResponse();
    }

    public async Task<Document?> GetDocument(Guid meldingId, Guid documentId)
    {
        return await _httpClient.GetFromJsonAsync<Document>(
            $"meldinger/{meldingId}/documents/{documentId}",
            _jsonSerializerOptions
        );
    }

    public async Task<Stream> DownloadDocument(Guid meldingId, Guid documentId)
    {
        var response = await _httpClient.GetAsync(
            $"meldinger/{meldingId}/documents/{documentId}/download"
        );

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync();
    }

    public async Task<ConsumerManifest> SubscribeConsumer(ConsumerManifest consumerManifest)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "subscriptions",
            consumerManifest,
            _jsonSerializerOptions
        );

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ConsumerManifest>(_jsonSerializerOptions)
            ?? throw new InvalidOperationException(
                "Could not parse response model to ConsumerManifest"
            );
    }
}
