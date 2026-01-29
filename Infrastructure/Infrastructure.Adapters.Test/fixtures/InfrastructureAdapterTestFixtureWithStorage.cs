using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures;

public class InfrastructureAdapterTestFixtureWithStorage : TestBedFixture, IAsyncLifetime
{
    internal readonly StorageFixture StorageFixture;

    private InfrastructureConfiguration _infraConfigMock =
        Substitute.For<InfrastructureConfiguration>();

    public InfrastructureAdapterTestFixtureWithStorage()
    {
        StorageFixture = new StorageFixture();
    }

    protected override void AddServices(
        IServiceCollection services,
        global::Microsoft.Extensions.Configuration.IConfiguration? configuration
    )
    {
        _infraConfigMock.PostgresConfiguration.Returns(
            new PostgresConfiguration { ConnectionString = "" }
        );
        _infraConfigMock.DocumentStorageConfiguration.Returns(
            new DocumentStorageConfiguration
            {
                AuthRequired = false,
                BaseUrl = StorageFixture.StorageBaseUrl,
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
        return StorageFixture.InitializeAsync();
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return StorageFixture.DisposeAsync();
    }
}
