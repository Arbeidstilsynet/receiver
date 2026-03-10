using Arbeidstilsynet.MeldingerReceiver.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;

public class InfrastructureAdapterTestFixture : TestBedFixture, IAsyncLifetime
{
    public InfrastructureAdapterTestFixture() { }

    private InfrastructureConfiguration _infraConfigMock =
        Substitute.For<InfrastructureConfiguration>();

    protected override void AddServices(
        IServiceCollection services,
        global::Microsoft.Extensions.Configuration.IConfiguration? configuration
    )
    {
        _infraConfigMock.DocumentStorageConfiguration.Returns(
            new DocumentStorageConfiguration
            {
                AuthRequired = false,
                BaseUrl = "",
                BucketName = StorageFixture.TestBucketName,
            }
        );
        services.AddInfrastructure(_infraConfigMock);
    }

    protected override ValueTask DisposeAsyncCore() => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.json", IsOptional = true };
    }

    ValueTask IAsyncLifetime.InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
