using System.ComponentModel.DataAnnotations;
using Arbeidstilsynet.Common.AspNetCore.Extensions.CrossCutting;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;

namespace Arbeidstilsynet.MeldingerReceiver.App;

internal record AppSettings
{
    [ConfigurationKeyName("API")]
    [Required]
    public required ApiConfiguration ApiConfig { get; init; }

    [Required]
    [ConfigurationKeyName("Infrastructure")]
    public required InfrastructureConfiguration InfrastructureConfig { get; init; }

    [ConfigurationKeyName("Domain")]
    [Required]
    public required DomainConfiguration DomainConfig { get; init; }
}

internal record ApiConfiguration
{
    [ConfigurationKeyName("Cors")]
    public CorsConfiguration Cors { get; init; } = new();

    [ConfigurationKeyName("Authentication")]
    [Required]
    public required AuthConfiguration AuthenticationConfiguration { get; init; }
}
