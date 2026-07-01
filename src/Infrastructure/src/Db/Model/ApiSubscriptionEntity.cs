using System.ComponentModel.DataAnnotations.Schema;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;

[Table("registered_default_apps", Schema = "public")]
internal class ApiSubscriptionEntity : BaseEntity
{
    public required Guid Id { get; init; }

    public required string AppIdentifier { get; init; }

    public required Guid SubscriptionEntityId { get; set; }

    public SubscriptionEntity? SubscriptionEntity { get; init; }
}
