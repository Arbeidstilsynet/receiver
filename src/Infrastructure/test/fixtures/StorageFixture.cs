using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;

public class StorageFixture : IAsyncLifetime
{
    private const ushort StoragePort = 4443;

    public static string TestBucketName = "test-bucket";

    private static readonly CreateBucketDto DefaultBucketConfig = new()
    {
        Name = TestBucketName,
        Location = "EU",
        StorageClass = "STANDARD",
        IamConfiguration = new IamConfiguration
        {
            UniformBucketLevelAccess = new UniformBucketLevelAccess { Enabled = true },
        },
    };

    private readonly IContainer _storageContainer;

    public StorageFixture()
    {
        _storageContainer = new ContainerBuilder()
            .WithImage("fsouza/fake-gcs-server:latest")
            .WithPortBinding(StoragePort, true)
            .WithCommand("-scheme", "http", "-backend", "memory")
            .WithTmpfsMount("/data")
            .Build();
    }

    public string StorageBaseUrl =>
        $"http://{_storageContainer.Hostname}:{_storageContainer.GetMappedPublicPort(StoragePort)}/storage/v1/";

    public async ValueTask DisposeAsync()
    {
        await _storageContainer.StopAsync();
        await _storageContainer.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await _storageContainer.StartAsync();
        await _storageContainer.CopyAsync(
            Encoding.UTF8.GetBytes(
                $"{{\"externalUrl\": \"http://{_storageContainer.Hostname}:{_storageContainer.GetMappedPublicPort(StoragePort)}\"}}"
            ),
            "/tmp/update-external-url.json"
        );
        await _storageContainer.CopyAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DefaultBucketConfig)),
            "/tmp/create-bucket.json"
        );
        await _storageContainer.ExecAsync(["/bin/sh", "-c", "apk add curl"]);
        await _storageContainer.ExecAsync([
            "/bin/sh",
            "-c",
            $"curl -X PUT --data-binary @/tmp/update-external-url.json -H \"Content-Type: application/json\" http://localhost:{StoragePort}/_internal/config",
        ]);
        await _storageContainer.ExecAsync([
            "/bin/sh",
            "-c",
            $"curl -X POST --data-binary @/tmp/create-bucket.json -H \"Content-Type: application/json\" http://localhost:{StoragePort}/storage/v1/b?project=localhost",
        ]);
    }
}
