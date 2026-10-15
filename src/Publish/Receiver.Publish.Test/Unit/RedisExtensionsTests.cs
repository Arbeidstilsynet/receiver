using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Implementation;
using Arbeidstilsynet.Receiver.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

namespace Arbeidstilsynet.Receiver.Test;

public class RedisExtensionsTests : IDisposable
{
    private readonly IMeldingerClient _client = Substitute.For<IMeldingerClient>();
    private readonly IMeldingerConsumer _consumer = Substitute.For<IMeldingerConsumer>();
    private readonly IMeldingerConsumer _scopedConsumer = Substitute.For<IMeldingerConsumer>();
    private readonly ServiceProvider _services;

    public RedisExtensionsTests()
    {
        _services = new ServiceCollection()
            .AddMetrics()
            .AddSingleton<ApiMeters>()
            .AddScoped<IMeldingerClient>(_ => _client)
            .AddScoped<IMeldingerConsumer>(_ => _scopedConsumer)
            .BuildServiceProvider();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task ConsumeNotifications_WithCurrentMelding_PassesRefetchedMeldingToConsumer(
        int maxConcurrency,
        bool fromRedrive
    )
    {
        var snapshot = CreateMelding();
        var current = snapshot with
        {
            MainContentId = Guid.NewGuid(),
            StructuredDataId = Guid.NewGuid(),
            AttachmentIds = [Guid.NewGuid()],
            Tags = new() { ["status"] = "updated" },
            InternalTags = new() { ["updated"] = "true" },
        };
        _client.GetMelding(snapshot.Id).Returns(current);
        _consumer.MaxConcurrency.Returns(maxConcurrency);
        RedisValue messageId = "1-0";

        var (successful, unsuccessful) = await _consumer.ConsumeNotifications(
            _services.GetRequiredService<IServiceScopeFactory>(),
            new() { [messageId] = snapshot },
            _services.GetRequiredService<ApiMeters>(),
            NullLogger.Instance,
            fromRedrive
        );

        successful.ShouldBe([messageId]);
        unsuccessful.ShouldBeEmpty();
        await _client.Received(1).GetMelding(snapshot.Id);
        var effectiveConsumer = maxConcurrency > 1 ? _scopedConsumer : _consumer;
        await effectiveConsumer
            .Received(1)
            .ConsumeMelding(Arg.Is<Melding>(m => ReferenceEquals(m, current)));
        var unusedConsumer = maxConcurrency > 1 ? _consumer : _scopedConsumer;
        await unusedConsumer.DidNotReceive().ConsumeMelding(Arg.Any<Melding>());
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData("id", false)]
    [InlineData("application", false)]
    [InlineData("source", false)]
    [InlineData("fetch", false)]
    [InlineData("missing", true)]
    [InlineData("id", true)]
    [InlineData("application", true)]
    [InlineData("source", true)]
    [InlineData("fetch", true)]
    public async Task ConsumeNotifications_WhenRefetchFails_DoesNotConsumeOrAcknowledge(
        string failure,
        bool fromRedrive
    )
    {
        var snapshot = CreateMelding();
        var current = failure switch
        {
            "missing" => null,
            "id" => snapshot with { Id = Guid.NewGuid() },
            "application" => snapshot with { ApplicationId = "another-app" },
            "source" => snapshot with { Source = MessageSource.Api },
            _ => snapshot,
        };
        _client
            .GetMelding(snapshot.Id)
            .Returns(
                failure == "fetch"
                    ? Task.FromException<Melding?>(
                        new HttpRequestException("Receiver unavailable.")
                    )
                    : Task.FromResult(current)
            );
        RedisValue messageId = "1-0";

        var (successful, unsuccessful) = await _consumer.ConsumeNotifications(
            _services.GetRequiredService<IServiceScopeFactory>(),
            new() { [messageId] = snapshot },
            _services.GetRequiredService<ApiMeters>(),
            NullLogger.Instance,
            fromRedrive
        );

        successful.ShouldBeEmpty();
        var error = unsuccessful.ShouldHaveSingleItem();
        error.MeldingId.ShouldBe(snapshot.Id);
        error.ValkeyMessageId.ShouldBe((string)messageId!);
        error.ExceptionMessage.ShouldBe(
            failure switch
            {
                "missing" => $"Melding {snapshot.Id} no longer exists in receiver.",
                "fetch" => "Receiver unavailable.",
                _ => $"Receiver identity changed for melding {snapshot.Id}.",
            }
        );
        await _consumer.DidNotReceive().ConsumeMelding(Arg.Any<Melding>());
        await _scopedConsumer.DidNotReceive().ConsumeMelding(Arg.Any<Melding>());
    }

    [Fact]
    public async Task ConsumeNotifications_WhenOneRefetchFails_StillConsumesOtherNotifications()
    {
        var missing = CreateMelding();
        var existing = CreateMelding();
        _client.GetMelding(missing.Id).Returns((Melding?)null);
        _client.GetMelding(existing.Id).Returns(existing);
        RedisValue missingId = "1-0";
        RedisValue existingId = "2-0";

        var (successful, unsuccessful) = await _consumer.ConsumeNotifications(
            _services.GetRequiredService<IServiceScopeFactory>(),
            new() { [missingId] = missing, [existingId] = existing },
            _services.GetRequiredService<ApiMeters>(),
            NullLogger.Instance
        );

        successful.ShouldBe([existingId]);
        unsuccessful.ShouldHaveSingleItem().MeldingId.ShouldBe(missing.Id);
        await _consumer.Received(1).ConsumeMelding(existing);
    }

    private static Melding CreateMelding() =>
        new()
        {
            Id = Guid.NewGuid(),
            ApplicationId = "test-app",
            Source = MessageSource.Altinn,
            ReceivedAt = DateTime.UtcNow,
        };

    public void Dispose() => _services.Dispose();
}
