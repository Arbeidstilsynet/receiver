namespace Arbeidstilsynet.MeldingerReceiver.Domain.Data;

public enum MessageSource
{
    /// <summary>
    ///     Represents the Altinn source, used for meldinger which come from the Altinn platform.
    /// </summary>
    Altinn,

    /// <summary>
    ///     Represents the Api source, used for meldinger which come in via our internal API endpoint.
    /// </summary>
    Api,
}
