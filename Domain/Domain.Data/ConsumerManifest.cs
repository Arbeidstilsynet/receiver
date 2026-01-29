namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

/// <summary>
/// A declaration of a consumer application, and its data sources
/// </summary>
public record ConsumerManifest
{
    public string ConsumerName { get; init; }

    public List<AppRegistration> AppRegistrations { get; init; }
}

public record AppRegistration
{
    public string AppId { get; init; }

    public MessageSource MessageSource { get; init; }
}
