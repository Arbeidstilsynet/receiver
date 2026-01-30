using Arbeidstilsynet.Receiver.Implementation;
using Arbeidstilsynet.Receiver.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace Arbeidstilsynet.Receiver.DependencyInjection;

/// <summary>
/// Configuration for Valkey (Redis) connection.
/// </summary>
public record ValkeyConfiguration
{
    /// <summary>
    /// The connection string for Valkey (Redis).
    /// </summary>
    public required string ConnectionString { get; init; } = "localhost";
}

/// <summary>
/// Configuration for MeldingerReceiver API endpoint.
/// </summary>
public record MeldingerReceiverApiConfiguration
{
    /// <summary>
    /// The base URL for the MeldingerReceiver API.
    /// </summary>
    public required string BaseUrl { get; init; } = "http://localhost:9008";
}

/// <summary>
///     Extensions for Dependency Injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    internal const string MeldingerReceiverApiClientKey = "MeldingerReceiverApiClient";

    /// <summary>
    /// Registers MeldingerReceiver with background service and dependencies in the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="valkeyConfiguration">Configuration for Valkey (Redis) connection.</param>
    /// <param name="meldingerReceiverApiConfiguration">Configuration for MeldingerReceiver API endpoint.</param>
    /// <param name="meldingerConsumerProvider">Factory for creating <see cref="IMeldingerConsumer" /> instances.</param>
    /// <returns>The <see cref="IServiceCollection" /> for chaining.</returns>
    public static IServiceCollection AddMeldingerReceiverWithBackgroundService(
        this IServiceCollection services,
        ValkeyConfiguration valkeyConfiguration,
        MeldingerReceiverApiConfiguration meldingerReceiverApiConfiguration,
        Func<ServiceProvider, IMeldingerConsumer> meldingerConsumerProvider
    )
    {
        services.AddSingleton(meldingerConsumerProvider);
        services.AddHostedService<ReceiverListener>();
        services.AddMeldingerReceiver(valkeyConfiguration, meldingerReceiverApiConfiguration);
        return services;
    }

    /// <summary>
    /// Registers MeldingerReceiver and dependencies in the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="valkeyConfiguration">Configuration for Valkey (Redis) connection.</param>
    /// <param name="meldingerReceiverApiConfiguration">Configuration for MeldingerReceiver API endpoint.</param>
    /// <returns>The <see cref="IServiceCollection" /> for chaining.</returns>
    public static IServiceCollection AddMeldingerReceiver(
        this IServiceCollection services,
        ValkeyConfiguration valkeyConfiguration,
        MeldingerReceiverApiConfiguration meldingerReceiverApiConfiguration
    )
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(
                valkeyConfiguration.ConnectionString,
                options => options.AbortOnConnectFail = false
            )
        );
        services.AddSingleton<ApiMeters>();
        services.AddOpenTelemetry().WithMetrics(options => options.AddMeter(ApiMeters.MeterName));
        services.AddScoped<IValkeyConsumer, ValkeyConsumer>();
        services.AddScoped<IMeldingerClient, MeldingerClient>();
        services.AddScoped<IMeldingerAdapter, MeldingerAdapter>();
        services.AddScoped<IMeldingerRedriver, MeldingerRedriver>();

        services
            .AddHttpClient(
                MeldingerReceiverApiClientKey,
                client =>
                {
                    client.BaseAddress = new Uri(meldingerReceiverApiConfiguration.BaseUrl);
                }
            )
            .AddStandardResilienceHandler();

        return services;
    }

    public static IServiceCollection AddReceiverInstrumentation(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithTracing(options =>
            {
                options.AddSource("AT.Common.MeldingerReceiver");
                options.AddRedisInstrumentation();
            });
        return services;
    }

    /// <summary>
    /// Registers and exposes endpoints which can be used to redrive/check messages on the meldinger-stream.
    /// </summary>
    public static RouteHandlerBuilder MapCommonRedriveEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/redrive-actions"
    )
    {
        var group = endpoints.MapGroup(pattern).WithTags("Redrive Actions");

        group.MapGet(
            "/get-pending-messages",
            (IMeldingerRedriver redriver) =>
            {
                return redriver.GetPendingMessages();
            }
        );
        group.MapPost(
            "/acknowledge-pending-messages",
            async (IMeldingerRedriver redriver, [FromQuery] string[]? messageIds) =>
            {
                if (messageIds == null || messageIds.Length == 0)
                {
                    var result = await redriver.GetPendingMessages();
                    return await redriver.AcknowledgePendingMessages([
                        .. result.Select(s => (MessageId)s.Key),
                    ]);
                }
                return await redriver.AcknowledgePendingMessages([.. messageIds]);
            }
        );
        return group.MapPost(
            "/redrive-pending-messages",
            (IMeldingerRedriver redriver) =>
            {
                return redriver.RedrivePendingMessages();
            }
        );
    }
}
