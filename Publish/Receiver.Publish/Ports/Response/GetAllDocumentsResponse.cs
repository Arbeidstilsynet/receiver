using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.Receiver.Model.Response;

public record GetAllDocumentsResponse
{
    public List<Document> Documents { get; init; } = [];
}
