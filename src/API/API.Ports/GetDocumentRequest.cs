namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public class GetDocumentRequest
{
    public Guid MeldingId { get; set; }
    public Guid DocumentId { get; set; }
}
