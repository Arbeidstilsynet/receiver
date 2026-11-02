using Arbeidstilsynet.Common.Altinn.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test;

public class AltinnConfigurationTests
{
    [Fact]
    public void Bind_WhenAuthenticationLevelIsOmitted_DefaultsToFour()
    {
        var settings = CreateConfiguration().Build().Get<AppSettings>();

        settings.ShouldNotBeNull();
        settings.InfrastructureConfig.AltinnConfiguration.Environment.ShouldBe(
            AltinnEnvironment.Local
        );
        settings.InfrastructureConfig.AltinnConfiguration.LocalAuthenticationLevel.ShouldBe(4);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("3", 3)]
    [InlineData("4", 4)]
    public void Bind_WhenAuthenticationLevelIsOverridden_UsesTestValue(
        string configuredLevel,
        int expectedLevel
    )
    {
        var configuration = CreateConfiguration()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Infrastructure:AltinnConfiguration:LocalAuthenticationLevel"] =
                        configuredLevel,
                }
            );

        var settings = configuration.Build().Get<AppSettings>();

        settings.ShouldNotBeNull();
        settings.InfrastructureConfig.AltinnConfiguration.LocalAuthenticationLevel.ShouldBe(
            expectedLevel
        );
        var defaultSettings = CreateConfiguration().Build().Get<AppSettings>();
        defaultSettings.ShouldNotBeNull();
        defaultSettings.InfrastructureConfig.AltinnConfiguration.LocalAuthenticationLevel.ShouldBe(
            4
        );
    }

    private static IConfigurationBuilder CreateConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Infrastructure:AltinnConfiguration:Environment"] = "Local",
            }
        );
}
