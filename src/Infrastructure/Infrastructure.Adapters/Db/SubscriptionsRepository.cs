using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;

internal class SubscriptionsRepository(ReceiverDbContext dbContext, IMapper mapper)
    : ISubscriptionsRepository
{
    private ReceiverDbContext DbContext
    {
        get
        {
            dbContext.Database.EnsureCreated();
            return dbContext;
        }
    }

    public async Task<IEnumerable<AltinnConnection>> CreateSubscription(
        ConsumerManifest consumerManifest
    )
    {
        var newSubscriptionEntityId = Guid.NewGuid();
        var altinnAppReferences = consumerManifest
            .AppRegistrations.Where(r => r.MessageSource == MessageSource.Altinn)
            .Select(s => new AltinnSubscriptionEntity
            {
                Id = Guid.NewGuid(),
                AppIdentifier = s.AppId,
                SubscriptionEntityId = newSubscriptionEntityId,
            })
            .ToList();
        var apiAppReferences = consumerManifest
            .AppRegistrations.Where(r => r.MessageSource == MessageSource.Api)
            .Select(s => new ApiSubscriptionEntity
            {
                Id = Guid.NewGuid(),
                AppIdentifier = s.AppId,
                SubscriptionEntityId = newSubscriptionEntityId,
            })
            .ToList();
        var subscriptionEntity = new SubscriptionEntity
        {
            Id = newSubscriptionEntityId,
            ConsumerName = consumerManifest.ConsumerName,
            RegisteredAltinnApps = altinnAppReferences,
            RegisteredApiApps = apiAppReferences,
        };
        var updatedEntity = await DbContext.Subscriptions.AddAsync(subscriptionEntity);

        await DbContext.SaveChangesAsync();
        await updatedEntity.ReloadAsync();

        return updatedEntity.Entity.RegisteredAltinnApps.Select(mapper.Map<AltinnConnection>);
    }

    public async Task<ConsumerManifest?> GetPersistedSubscription(string consumerName)
    {
        var result = await DbContext
            .Subscriptions.Include(i => i.RegisteredAltinnApps.Where(a => a.SubscriptionId != null))
            .Include(i => i.RegisteredApiApps)
            .Where(w => w.ConsumerName == consumerName)
            .SingleOrDefaultAsync();

        return result == null ? null : mapper.Map<ConsumerManifest>(result);
    }

    public async Task<IEnumerable<AltinnConnection>> GetAllActiveAltinnSubscriptions()
    {
        return await DbContext
            .AltinnApps.Where(w => w.SubscriptionId != null)
            .Select(s => mapper.Map<AltinnConnection>(s))
            .ToListAsync();
    }

    public async Task<AltinnConnection?> GetActiveAltinnSubscription(string altinnAppId)
    {
        var result = await DbContext
            .AltinnApps.AsNoTracking()
            .Where(w => w.AppIdentifier == altinnAppId)
            .FirstOrDefaultAsync();
        return result == null ? null : mapper.Map<AltinnConnection>(result);
    }

    public async Task UpdateAltinnSubscriptionId(Guid altinnSubscriptionEntity, int subscriptionId)
    {
        var entityToUpdate = await DbContext.AltinnApps.FindAsync(altinnSubscriptionEntity);
        if (entityToUpdate != null)
        {
            entityToUpdate.SubscriptionId = subscriptionId;
            await DbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteSubscription(ConsumerManifest consumerManifest)
    {
        var existingEntity = await DbContext
            .Subscriptions.Where(w => w.ConsumerName == consumerManifest.ConsumerName)
            .SingleOrDefaultAsync();
        if (existingEntity is not null)
        {
            // Rely on database cascade delete (FK `SubscriptionEntityId` is required and
            // configured with DeleteBehavior.Cascade in migrations/model).
            DbContext.Subscriptions.Remove(existingEntity);
            await DbContext.SaveChangesAsync();

            // Clear change tracker to prevent caching issues
            DbContext.ChangeTracker.Clear();
        }
    }

    public async Task<IList<ConsumerManifest>> GetSubscriptions()
    {
        return await DbContext
            .Subscriptions.Include(i => i.RegisteredAltinnApps)
            .Include(i => i.RegisteredApiApps)
            .OrderBy(o => o.ConsumerName)
            .Select(s => mapper.Map<ConsumerManifest>(s))
            .ToListAsync();
    }

    public async Task<AppRegistration?> GetActiveAppRegistration(
        MessageSource messageSource,
        string appId
    )
    {
        if (messageSource == MessageSource.Altinn)
        {
            var altinnApp = await DbContext
                .AltinnApps.Where(w => w.AppIdentifier == appId)
                .FirstOrDefaultAsync();
            return altinnApp == null
                ? null
                : new AppRegistration { AppId = appId, MessageSource = MessageSource.Altinn };
        }
        else if (messageSource == MessageSource.Api)
        {
            var apiApp = await DbContext
                .ApiApps.Where(w => w.AppIdentifier == appId)
                .FirstOrDefaultAsync();
            return apiApp == null
                ? null
                : new AppRegistration { AppId = appId, MessageSource = MessageSource.Api };
        }
        else
        {
            throw new InvalidOperationException("Provided unkown message source");
        }
    }
}
