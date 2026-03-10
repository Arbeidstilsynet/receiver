namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;

public record AltinnEventsSubscription
{
    public required int Id { get; init; }
    public required string CallbackUrl { get; set; }
    public required string SourceFilter { get; set; }
    public required string CreatedBy { get; set; }
    public required string Consumer { get; set; }
    public required DateTime Created { get; set; }
}
