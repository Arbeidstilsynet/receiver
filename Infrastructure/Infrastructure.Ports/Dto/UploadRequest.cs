using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

public record UploadRequest
{
    public required Document Document { get; init; }
    public required Stream InputStream { get; init; }
}
