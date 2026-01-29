namespace Arbeidstilsynet.Meldinger.Receiver.Ports.Model;

/// <summary>
/// Represents the result of a redrive operation, including counts of successful and unsuccessful attempts,
/// as well as a list of exceptions encountered during the process.
/// </summary>
public record RedriveResult
{
    /// <summary>
    /// Gets the number of successfully redriven items.
    /// </summary>
    public int SuccessfulCount { get; init; }

    /// <summary>
    /// Gets the number of items that failed to redrive.
    /// </summary>
    public int UnsuccessfulCount { get; init; }

    /// <summary>
    /// Gets the list of exceptions that occurred during the redrive operation.
    /// </summary>
    public List<RedriveException> RedriveExceptions { get; init; }
}
