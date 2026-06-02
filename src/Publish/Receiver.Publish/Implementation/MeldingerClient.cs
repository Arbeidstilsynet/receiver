using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.DependencyInjection;
using Arbeidstilsynet.Receiver.Model;
using Arbeidstilsynet.Receiver.Model.Response;
using Arbeidstilsynet.Receiver.Ports;

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

    public async Task<Melding?> GetMelding(Guid meldingId)
    {
        var response = await _httpClient.GetFromJsonAsync<GetMeldingResponse>(
            $"meldinger/{meldingId}",
            _jsonSerializerOptions
        );
        return response?.Melding;
    }

    public async Task<Melding?> GetMeldingByShortId(string shortId)
    {
        var response = await _httpClient.GetAsync($"meldinger/by-short-id/{shortId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict =
                await response.Content.ReadFromJsonAsync<GetMeldingByShortIdConflictResponse>(
                    _jsonSerializerOptions
                );
            throw new MeldingShortIdCollisionException(
                shortId,
                conflict?.MatchingMeldingIds ?? []
            );
        }

        response.EnsureSuccessStatusCode();

        var melding = await response.Content.ReadFromJsonAsync<GetMeldingResponse>(
            _jsonSerializerOptions
        );
        return melding?.Melding;
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
