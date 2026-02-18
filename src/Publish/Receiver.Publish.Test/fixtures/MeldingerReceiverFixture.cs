using Arbeidstilsynet.Receiver.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.Receiver.Adapters.Test.fixtures;

public class MeldingerReceiverFixture : TestBedFixture, IAsyncLifetime
{
    private readonly ValkeyFixture _ValkeyFixture;

    public MeldingerReceiverFixture()
    {
        _ValkeyFixture = new ValkeyFixture();
    }

    protected override void AddServices(
        IServiceCollection services,
        global::Microsoft.Extensions.Configuration.IConfiguration? configuration
    )
    {
        services.AddMeldingerReceiver(
            new ValkeyConfiguration { ConnectionString = _ValkeyFixture.ValkeyBaseUrl },
            new MeldingerReceiverApiConfiguration { BaseUrl = "http://localhost:9008" }
        );
    }

    protected override ValueTask DisposeAsyncCore() => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.json", IsOptional = true };
    }

    ValueTask IAsyncLifetime.InitializeAsync()
    {
        return _ValkeyFixture.InitializeAsync();
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _ValkeyFixture.DisposeAsync();
    }
}
