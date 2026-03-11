using System.Text.Json.Serialization;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.VirusScan;

internal record ScanResponse
{
    [JsonPropertyName("result")]
    public Status Result { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("filename")]
    public string? Filename { get; init; }

    [JsonPropertyName("virus")]
    public string? Virus { get; init; }
}
