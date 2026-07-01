using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;

[Table("registered_subscriptions", Schema = "public")]
[Index(nameof(ConsumerName))]
internal class SubscriptionEntity : BaseEntity
{
    public required Guid Id { get; init; }

    public required string ConsumerName { get; init; }

    public required List<AltinnSubscriptionEntity> RegisteredAltinnApps { get; set; } = [];

    public required List<ApiSubscriptionEntity> RegisteredApiApps { get; set; } = [];
}
