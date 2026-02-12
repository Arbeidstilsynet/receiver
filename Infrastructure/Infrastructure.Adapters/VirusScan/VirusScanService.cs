using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.VirusScan;

internal class VirusScanService : IVirusScanService
{
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<VirusScanService> _logger;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new JsonSerializerOptions() { Converters = { new JsonStringEnumConverter() } };

    public VirusScanService(
        IHttpClientFactory httpClientFactory,
        IDocumentStorage documentStorage,
        ILogger<VirusScanService> logger,
        IOptions<InfrastructureConfiguration> options
    )
    {
        _documentStorage = documentStorage;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("VirusScanServiceClient");
        _httpClient.BaseAddress = new Uri(options.Value.VirusScanConfiguration.BaseUrl);
    }

    public async Task<DocumentScanResult> ScanForVirus(
        UploadResponse uploadResponse,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var activity = Tracer.Source.StartActivity();
            using var httpContent = new DocumentDownloadHttpContent(
                _documentStorage,
                uploadResponse.PersistedDocument
            );
            using var multipartContent = new MultipartFormDataContent
            {
                { httpContent, "file", uploadResponse.PersistedDocument.FileName },
            };

            var response = await _httpClient.PostAsync(
                "api/v2/scan",
                multipartContent,
                cancellationToken
            );
            response.EnsureSuccessStatusCode();
            var scanResponseAsString = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Virus scan result was {ScanResponse}", scanResponseAsString);
            var scanResponseArray = JsonSerializer.Deserialize<ScanResponse[]>(
                scanResponseAsString,
                JsonSerializerOptions
            );
            var scanResponse =
                scanResponseArray?.FirstOrDefault(s =>
                    s.Filename == uploadResponse.PersistedDocument.FileName
                ) ?? scanResponseArray?[0];
            if (scanResponse is { Result: Status.OK })
            {
                return DocumentScanResult.Clean;
            }
            else if (scanResponse is { Result: Status.FOUND })
            {
                return DocumentScanResult.Infected;
            }
            
            return DocumentScanResult.Unknown;
            
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not scan file '{FileName}' for virus. Internal document id: {InternalDocumentId}",
                uploadResponse.PersistedDocument.FileName,
                uploadResponse.PersistedDocument.InternalDocumentReference
            );
        }
        
        return DocumentScanResult.Unknown;
    }

    private class DocumentDownloadHttpContent : HttpContent
    {
        private readonly IDocumentStorage _documentStorage;
        private readonly DocumentStorageDto _documentStorageDto;

        public DocumentDownloadHttpContent(
            IDocumentStorage documentStorage,
            DocumentStorageDto documentStorageDto
        )
        {
            _documentStorage = documentStorage;
            _documentStorageDto = documentStorageDto;

            var contentType = documentStorageDto.ContentType;
            Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrEmpty(contentType) ? "application/octet-stream" : contentType
            );
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return _documentStorage.Download(
                _documentStorageDto.InternalDocumentReference,
                stream,
                CancellationToken.None
            );
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken
        )
        {
            return _documentStorage.Download(
                _documentStorageDto.InternalDocumentReference,
                stream,
                cancellationToken: cancellationToken
            );
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1; // Length is unknown
            return false;
        }
    }
}
