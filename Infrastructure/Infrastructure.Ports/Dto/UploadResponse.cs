namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

public record UploadResponse
{
    public required DocumentStorageDto PersistedDocument { get; init; }
}
