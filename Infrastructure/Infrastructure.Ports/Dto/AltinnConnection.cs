namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

public record AltinnConnection
{
    public required Guid InternalId { get; init; }
    public required string AltinnAppId { get; init; }

    public int? SubscriptionId { get; init; }
};
