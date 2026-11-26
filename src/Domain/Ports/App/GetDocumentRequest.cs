namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;

public class GetDocumentRequest
{
    public Guid MeldingId { get; set; }
    public Guid DocumentId { get; set; }
}
