using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;

public class DomainConfiguration
{
    /// <summary>
    /// Sets a flag whether only registered / subscribed applications are allowed to interact with the system.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="true"/>, incoming requests or connections from applications that are not
    /// registered/subscribed in the configured application registry will be rejected. When <see langword="false"/>,
    /// we do receive any messages from any appId / messageSource combination.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> to restrict access to registered applications only; otherwise <see langword="false"/>.
    /// </value>
    [Required]
    public virtual required bool AllowOnlyRegisteredApps { get; init; }

    [Required]
    public virtual required bool RequireAltinnDeletionOnUnsubscribe { get; init; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(
        this IServiceCollection services,
        DomainConfiguration domainConfiguration
    )
    {
        services.AddMapper();
        services.AddSingleton(Options.Create(domainConfiguration));
        services.AddScoped<IMeldingService, MeldingService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        return services;
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
