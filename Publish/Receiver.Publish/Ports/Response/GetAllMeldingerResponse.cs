using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Meldinger.Receiver.Model.Response;

public class GetAllMeldingerResponse
{
    public required List<Melding> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalRecords { get; init; }
    public required int TotalPages { get; init; }
}
