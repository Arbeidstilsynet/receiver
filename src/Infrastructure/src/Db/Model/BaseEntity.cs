namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;

internal abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
