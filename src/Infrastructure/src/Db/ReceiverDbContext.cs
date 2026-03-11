using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;

internal class ReceiverDbContext(DbContextOptions<ReceiverDbContext> dbContextOption)
    : DbContext(dbContextOption)
{
    public DbSet<MeldingEntity> Meldinger { get; set; }
    public DbSet<DocumentEntity> Documents { get; set; }

    public DbSet<SubscriptionEntity> Subscriptions { get; set; }
    public DbSet<AltinnSubscriptionEntity> AltinnApps { get; set; }

    public DbSet<ApiSubscriptionEntity> ApiApps { get; set; }

    public override int SaveChanges()
    {
        AddTimestamps();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddQuartz(builder => builder.UsePostgreSql());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        AddTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddTimestamps()
    {
        var entities = ChangeTracker
            .Entries()
            .Where(x =>
                x.Entity is BaseEntity
                && (x.State == EntityState.Added || x.State == EntityState.Modified)
            );

        foreach (var entity in entities)
        {
            var now = DateTime.UtcNow; // current datetime

            if (
                entity.State == EntityState.Added
                && ((BaseEntity)entity.Entity).CreatedAt == default
            )
            {
                ((BaseEntity)entity.Entity).CreatedAt = now;
            }
            ((BaseEntity)entity.Entity).UpdatedAt = now;
        }
    }
}
