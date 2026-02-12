using System.Reflection;
using Arbeidstilsynet.Common.Altinn.DependencyInjection;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.AdHoc;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Altinn;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Notification;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Storage;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.VirusScan;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.AdHoc;
using Google.Cloud.Storage.V1;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;

public record InfrastructureConfiguration
{
    public virtual required PostgresConfiguration PostgresConfiguration { get; init; }

    public virtual required DocumentStorageConfiguration DocumentStorageConfiguration { get; init; }

    public virtual required ValkeyConfiguration ValkeyConfiguration { get; init; }

    public virtual required VirusScanConfiguration VirusScanConfiguration { get; init; }

    public virtual required MaskinportenConfiguration MaskinportenConfiguration { get; init; }

    public virtual required AltinnConfiguration AltinnConfiguration { get; init; }

    public virtual required string AppDomain { get; init; }
}

public record DocumentStorageConfiguration
{
    public string? BaseUrl { get; init; }

    public required bool AuthRequired { get; init; } = true;

    public required string BucketName { get; init; }
}

public record PostgresConfiguration
{
    public required string ConnectionString { get; init; }
}

public record ValkeyConfiguration
{
    public required string ConnectionString { get; init; }

    public string? StreamName { get; set; }

    public string? MessageKey { get; set; }
}

public record VirusScanConfiguration
{
    public required string BaseUrl { get; init; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        InfrastructureConfiguration infrastructureConfiguration
    )
    {
        services.AddScoped<IAdHocMigrateMainDocument, AdHocMigrateMainDocument>(); // TODO: Remove

        services.AddScoped<IMeldingRepository, MeldingRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IInternalDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentStorage, DocumentStorage>();
        services.AddScoped<IVirusScanService, VirusScanService>();
        services.AddScoped<IAltinnRegistrationService, AltinnRegistrationService>();
        services.AddScoped<IAltinnRecoveryService, AltinnRecoveryService>();
        services.AddScoped<ISubscriptionsRepository, SubscriptionsRepository>();
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<IPostMeldingPersistedAction, AltinnCompletionAction>();
        services.AddScoped<IPostMeldingPersistedAction, NotificationTriggerAction>();
        services.AddSingleton(Options.Create(infrastructureConfiguration));
        if (
            !string.IsNullOrEmpty(infrastructureConfiguration.ValkeyConfiguration?.ConnectionString)
        )
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(
                    infrastructureConfiguration.ValkeyConfiguration.ConnectionString,
                    options => options.AbortOnConnectFail = false
                )
            );
            services.AddScoped<IMeldingNotificationService, MeldingNotificationService>();
        }
        else
        {
            services.AddScoped<IMeldingNotificationService, DummyMeldingNotificationService>();
        }

        services.AddDbContext<ReceiverDbContext>(opt =>
        {
            opt.UseNpgsql(
                infrastructureConfiguration.PostgresConfiguration.ConnectionString,
                options =>
                {
                    options.MigrationsHistoryTable("ef_migrations_history");
                }
            );
        });
        var storageClient = new StorageClientBuilder
        {
            BaseUri = infrastructureConfiguration.DocumentStorageConfiguration.BaseUrl,
            UnauthenticatedAccess = !infrastructureConfiguration
                .DocumentStorageConfiguration
                .AuthRequired,
        }.Build();
        services.AddSingleton(storageClient);

        services.AddMapper();

        return services;
    }

    public static IHealthChecksBuilder AddInfrastructureHealthChecks(
        this IHealthChecksBuilder healthCheckBuilder
    )
    {
        return healthCheckBuilder
            .AddDbContextCheck<ReceiverDbContext>("DbContextCheck")
            .AddCheck<CloudStorageHealthCheck>("CloudStorageCheck")
            .AddCheck<ValkeyHealthCheck>("ValkeyCheck");
    }

    internal static IServiceCollection AddMapper(this IServiceCollection services)
    {
        var existingConfig = services
            .Select(s => s.ImplementationInstance)
            .OfType<TypeAdapterConfig>()
            .FirstOrDefault();

        if (existingConfig == null)
        {
            var config = new TypeAdapterConfig()
            {
                RequireExplicitMapping = false,
                RequireDestinationMemberSource = true,
            };
            config.Scan(Assembly.GetExecutingAssembly());
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();
        }
        else
        {
            existingConfig.Scan(Assembly.GetExecutingAssembly());
        }

        return services;
    }
}
