namespace Arbeidstilsynet.Meldinger.Receiver.Ports.Model;

/// <summary>
/// Represents an exception that occurred during the redrive process of a message.
/// </summary>
public record RedriveException
{
    /// <summary>
    /// Gets the valkey message id which can be used to acknowledge a message.
    /// </summary>
    public string ValkeyMessageId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the received melding associated with the exception.
    /// </summary>
    public Guid MeldingId { get; init; }

    /// <summary>
    /// Gets the traceId of the redrive operation.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets the traceId of the origin trace when the message was send in the first time.
    /// </summary>
    public string? OriginalTraceId { get; init; }

    /// <summary>
    /// Gets the message describing the exception that occurred.
    /// </summary>
    public string? ExceptionMessage { get; init; }
}
