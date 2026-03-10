using System.ComponentModel.DataAnnotations.Schema;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;

[Table("meldinger", Schema = "public")]
internal class MeldingEntity : BaseEntity
{
    public required Guid Id { get; set; }

    [Column(TypeName = "varchar(24)")]
    public required MessageSource Source { get; set; }

    public required string ApplicationId { get; set; }

    public required DateTime ReceivedAt { get; set; }

    public Dictionary<string, string> Tags { get; set; } = []; // Fylt ut av avsender
    public Dictionary<string, string> InternalTags { get; set; } = []; // Fylles ut her i denne applikasjonen

    public List<DocumentEntity> Documents { get; set; } = [];
}
