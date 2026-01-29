namespace Arbeidstilsynet.MeldingerReceiver.API.Ports.Event;

// TODO: Tankekors: I hvilket namespace skal denne ligge? Det ligger en med samme navn i Infrastructure.Ports.

/// <summary>
/// Meldingen er tatt imot og prosessert i mottaket. Venter kanskje fortsatt på at mottaker skal hente den.
/// </summary>
public record MeldingProcessedEvent
{
    public Guid MeldingId { get; init; }
    public string Avsender { get; init; }
    public string Mottaker { get; init; }
}
