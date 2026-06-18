using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

public record UploadRequest
{
    public required Document Document { get; init; }
    public required Stream InputStream { get; init; }
}
