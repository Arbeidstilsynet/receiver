using System.ComponentModel.DataAnnotations.Schema;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;

[Table("documents", Schema = "public")]
internal class DocumentEntity : BaseEntity
{
    public required Guid Id { get; set; }

    public required Guid MeldingId { get; set; }

    public MeldingEntity? Melding { get; init; }

    public required string InternalDocumentReference { get; set; }

    public required DocumentType DocumentType { get; set; }

    public Dictionary<string, string> Tags { get; set; } = [];

    public string? ContentType { get; set; } = null;

    public string? FileName { get; set; } = null;

    [Column(TypeName = "varchar(24)")]
    public required DocumentScanResult ScanResult { get; set; }
}
