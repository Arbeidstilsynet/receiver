namespace Arbeidstilsynet.MeldingerReceiver.API.Ports;

public class EditMeldingRequest
{
    public required Guid MeldingId { get; set; }
    
    public Guid? MainContentId { get; set; }
    public Guid? StructuredDataId { get; set; }
    public List<Guid>? AttachmentReferenceIds { get; set; }
    
    public Dictionary<string, string?>? MetadataUpdates { get; set; }
}