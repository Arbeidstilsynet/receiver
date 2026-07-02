namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

public record UploadResponse
{
    public required DocumentStorageDto PersistedDocument { get; init; }
}
