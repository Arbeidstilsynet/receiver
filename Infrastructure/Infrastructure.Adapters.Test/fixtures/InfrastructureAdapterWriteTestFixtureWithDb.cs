using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;
using Xunit.v3;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures;

public class InfrastructureAdapterWriteTestFixtureWithDb : TestBedFixture, IAsyncLifetime
{
    private readonly PostgresDbDemoFixture _dbDemoFixture;

    private readonly InfrastructureConfiguration _infraConfigMock =
        Substitute.For<InfrastructureConfiguration>();

    public InfrastructureAdapterWriteTestFixtureWithDb()
    {
        _dbDemoFixture = new PostgresDbDemoFixture();
    }

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
        _infraConfigMock.PostgresConfiguration.Returns(
            new PostgresConfiguration { ConnectionString = _dbDemoFixture.ConnectionString }
        );
        services.AddInfrastructure(_infraConfigMock);
    }

    protected override ValueTask DisposeAsyncCore()
    {
        return _dbDemoFixture.DisposeAsync();
    }

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.json", IsOptional = true };
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await _dbDemoFixture.InitializeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsyncCore();
    }
}
