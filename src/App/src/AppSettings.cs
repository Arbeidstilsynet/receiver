using System.ComponentModel.DataAnnotations;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;

namespace Arbeidstilsynet.MeldingerReceiver.App;

internal record AppSettings
{
    [ConfigurationKeyName("API")]
    public ApiConfiguration ApiConfig { get; init; } = new();

    [Required]
    [ConfigurationKeyName("Infrastructure")]
    public required InfrastructureConfiguration InfrastructureConfig { get; init; }

    [ConfigurationKeyName("Domain")]
    public required DomainConfiguration DomainConfig { get; init; }
}

internal record ApiConfiguration
{
    [ConfigurationKeyName("Cors")]
    public CorsConfiguration Cors { get; init; } = new();

    [ConfigurationKeyName("Authentication")]
    public AuthConfiguration AuthenticationConfiguration { get; init; } = new();
}

internal record CorsConfiguration
{
    [Required]
    public string[] AllowedOrigins { get; init; } = [];

    [Required]
    public bool AllowCredentials { get; init; } = false;
}

internal record AuthConfiguration
{
    [ConfigurationKeyName("DisableAuth")]
    public bool DisableAuth { get; init; } = true;

    /// <summary>
    /// required if <see cref="DisableAuth"/> is false
    /// </summary>
    [ConfigurationKeyName("TenantId")]
    public string EntraTenantId { get; init; } = string.Empty;

    /// <summary>
    /// required if <see cref="DisableAuth"/> is false
    /// </summary>
    [ConfigurationKeyName("ClientId")]
    public string EntraClientId { get; init; } = string.Empty;

    /// <summary>
    /// required if <see cref="DisableAuth"/> is false
    /// </summary>
    [ConfigurationKeyName("Scope")]
    public string EntraScope { get; init; } = string.Empty;
}
