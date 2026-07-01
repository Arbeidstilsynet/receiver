using System.ComponentModel.DataAnnotations.Schema;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;

[Table("registered_altinn_apps", Schema = "public")]
internal class AltinnSubscriptionEntity : BaseEntity
{
    public required Guid Id { get; init; }

    public required string AppIdentifier { get; init; }
    public int? SubscriptionId { get; set; }

    public required Guid SubscriptionEntityId { get; set; }

    public SubscriptionEntity? SubscriptionEntity { get; init; }
}
