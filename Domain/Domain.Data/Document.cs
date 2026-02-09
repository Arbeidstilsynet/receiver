namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public record Document
{
    public required Guid DocumentId { get; init; }
    public required Guid MeldingId { get; init; }
    public required FileMetadata FileMetadata { get; init; }
    public required DocumentScanResult ScanResult { get; init; }
    public Dictionary<string, string> Tags { get; init; } = [];
}

public record FileMetadata
{
    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}

public enum DocumentScanResult
{
    /// <summary>
    /// The file scan did not find any malware in the file.
    /// </summary>
    Clean,

    /// <summary>
    /// The file scan found malware in the file.
    /// </summary>
    Infected,

    /// <summary>
    /// The file scan result is unknown.
    /// </summary>
    Unknown,
}
