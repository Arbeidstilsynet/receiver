namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

/// <summary>
/// A declaration of a consumer application, and its data sources
/// </summary>
public record ConsumerManifest
{
    /// <summary>
    /// The unique name of the consumer application
    /// </summary>
    public required string ConsumerName { get; init; }

    /// <summary>
    /// The app registrations associated with this consumer
    /// </summary>
    public List<AppRegistration> AppRegistrations { get; init; } = [];
}

public record AppRegistration
{
    /// <summary>
    /// The AppId of the message source
    /// </summary>
    public required string AppId { get; init; }

    /// <summary>
    /// The message source
    /// </summary>
    public MessageSource MessageSource { get; init; }
}
