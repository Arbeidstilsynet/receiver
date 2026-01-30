using Microsoft.AspNetCore.Http;

namespace Arbeidstilsynet.Receiver.Model.Request;

/// <summary>
/// Request body for submitting a melding with file content and metadata.
/// </summary>
public record PostMeldingBody
{
    /// <summary>
    /// Unique identifier of the submitting application.
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// Additional metadata for the melding.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = [];

    /// <summary>
    /// Main content file for the melding.
    /// </summary>
    public required IFormFile MainContent { get; init; }

    /// <summary>
    /// Optional structured data file (e.g. JSON matching the contract of the consumer).
    /// </summary>
    public required IFormFile? StructuredData { get; init; }

    /// <summary>
    /// Optional attachment files.
    /// </summary>
    public List<IFormFile> Attachments { get; init; } = [];
}
