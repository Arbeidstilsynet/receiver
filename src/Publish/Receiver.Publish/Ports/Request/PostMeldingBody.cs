using System.ComponentModel.DataAnnotations;
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
    /// Main content file for the melding. This is usually a human-readable document (e.g. PDF).
    /// <br/>
    /// Allowed content types: Anything except application/json, which is reserved for the <see cref="StructuredData"/> property.
    /// </summary>
    public IFormFile? MainContent { get; init; }

    /// <summary>
    /// Optional structured data file. The only allowed type is: application/json
    /// </summary>
    public IFormFile? StructuredData { get; init; }

    /// <summary>
    /// Optional attachment files.
    /// </summary>
    public List<IFormFile> Attachments { get; init; } = [];
}
