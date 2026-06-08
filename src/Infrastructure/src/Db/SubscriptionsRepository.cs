using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;

internal class SubscriptionsRepository(ReceiverDbContext dbContext, IMapper mapper)
    : ISubscriptionsRepository
{
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
        var updatedEntity = await dbContext.Subscriptions.AddAsync(subscriptionEntity);

        await dbContext.SaveChangesAsync();
        await updatedEntity.ReloadAsync();

        return updatedEntity.Entity.RegisteredAltinnApps.Select(mapper.Map<AltinnConnection>);
    }

    public async Task<ConsumerManifest?> GetPersistedSubscription(string consumerName)
    {
        var result = await dbContext
            .Subscriptions.Include(i => i.RegisteredAltinnApps.Where(a => a.SubscriptionId != null))
            .Include(i => i.RegisteredApiApps)
            .Where(w => w.ConsumerName == consumerName)
            .SingleOrDefaultAsync();

        return result == null ? null : mapper.Map<ConsumerManifest>(result);
    }

    public async Task<IEnumerable<AltinnConnection>> GetAllActiveAltinnSubscriptions()
    {
        return await dbContext
            .AltinnApps.Where(w => w.SubscriptionId != null)
            .Select(s => mapper.Map<AltinnConnection>(s))
            .ToListAsync();
    }

    public async Task<AltinnConnection?> GetActiveAltinnSubscription(string altinnAppId)
    {
        var result = await dbContext
            .AltinnApps.AsNoTracking()
            .Where(w => w.AppIdentifier == altinnAppId)
            .FirstOrDefaultAsync();
        return result == null ? null : mapper.Map<AltinnConnection>(result);
    }

    public async Task<AltinnConnection?> GetAltinnConnectionByAltinnSubscriptionId(int altinnSubscriptionId)
    {
        var result = await dbContext.AltinnApps.AsNoTracking().Where(w => w.SubscriptionId == altinnSubscriptionId).FirstOrDefaultAsync();
        return result == null ? null : mapper.Map<AltinnConnection>(result);
    }

    public async Task UpdateAltinnSubscriptionId(Guid altinnSubscriptionEntity, int subscriptionId)
    {
        var entityToUpdate = await dbContext.AltinnApps.FindAsync(altinnSubscriptionEntity);
        if (entityToUpdate != null)
        {
            entityToUpdate.SubscriptionId = subscriptionId;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteSubscription(ConsumerManifest consumerManifest)
    {
        var existingEntity = await dbContext
            .Subscriptions.Where(w => w.ConsumerName == consumerManifest.ConsumerName)
            .SingleOrDefaultAsync();
        if (existingEntity is not null)
        {
            // Rely on database cascade delete (FK `SubscriptionEntityId` is required and
            // configured with DeleteBehavior.Cascade in migrations/model).
            dbContext.Subscriptions.Remove(existingEntity);
            await dbContext.SaveChangesAsync();

            // Clear change tracker to prevent caching issues
            dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<IList<ConsumerManifest>> GetSubscriptions()
    {
        return await dbContext
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
            var altinnApp = await dbContext
                .AltinnApps.Where(w => w.AppIdentifier == appId)
                .FirstOrDefaultAsync();
            return altinnApp == null
                ? null
                : new AppRegistration { AppId = appId, MessageSource = MessageSource.Altinn };
        }
        else if (messageSource == MessageSource.Api)
        {
            var apiApp = await dbContext
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
