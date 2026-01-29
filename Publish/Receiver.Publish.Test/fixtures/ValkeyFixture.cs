using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Arbeidstilsynet.Meldinger.Receiver.Adapters.Test.fixtures;

public class ValkeyFixture : IAsyncLifetime
{
    private readonly IContainer _valkeyContainer;

    const ushort ValkeyPort = 6379;

    public ValkeyFixture()
    {
        _valkeyContainer = new ContainerBuilder()
            .WithImage("valkey/valkey:latest")
            .WithPortBinding(ValkeyPort, true)
            .Build();
    }

    public string ValkeyBaseUrl =>
        $"{_valkeyContainer.Hostname}:{_valkeyContainer.GetMappedPublicPort(ValkeyPort)}";

    public ValueTask InitializeAsync()
    {
        return new ValueTask(_valkeyContainer.StartAsync());
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(_valkeyContainer.StopAsync());
    }
}
