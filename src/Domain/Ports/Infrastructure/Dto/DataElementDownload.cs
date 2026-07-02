namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

public record DataElementDownload
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    public string? Filename { get; init; }
}
