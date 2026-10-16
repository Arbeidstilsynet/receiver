using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Implementation;
using Arbeidstilsynet.Receiver.Ports;
using Arbeidstilsynet.Receiver.Ports.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

namespace Arbeidstilsynet.Receiver.Test;

public class MeldingerRedriverTests : IDisposable
{
    private readonly IValkeyConsumer _valkey = Substitute.For<IValkeyConsumer>();
    private readonly IMeldingerClient _client = Substitute.For<IMeldingerClient>();
    private readonly IMeldingerConsumer _consumer = Substitute.For<IMeldingerConsumer>();
    private readonly Dictionary<RedisValue, Melding> _pending = [];
    private readonly ServiceProvider _services;
    private readonly MeldingerRedriver _redriver;
    private readonly ConsumerManifest _manifest = new()
    {
        ConsumerName = "test-consumer",
        AppRegistrations =
        [
            new AppRegistration { AppId = "test-app", MessageSource = MessageSource.Altinn },
        ],
    };

    public MeldingerRedriverTests()
    {
        _consumer.ConsumerManifest.Returns(_manifest);
        _valkey.GetPendingMessagesAsync(_manifest).Returns(_ => new(_pending));
        _valkey
            .AcknowledgeMessagesAsync(_manifest, Arg.Any<IEnumerable<RedisValue>>())
            .Returns(call =>
            {
                var ids = call.ArgAt<IEnumerable<RedisValue>>(1).ToList();
                return (long)ids.Count(id => _pending.Remove(id));
            });
        _services = new ServiceCollection()
            .AddMetrics()
            .AddSingleton<ApiMeters>()
            .AddScoped<IMeldingerClient>(_ => _client)
            .BuildServiceProvider();
        _redriver = new(
            _services.GetRequiredService<IServiceScopeFactory>(),
            _valkey,
            _consumer,
            _services.GetRequiredService<ApiMeters>(),
            NullLogger<MeldingerRedriver>.Instance
        );
    }

    [Fact]
    public async Task RedriveSelected_ConsumesAndAcknowledgesOnlySelectedPendingMessages()
    {
        var selected = AddPending("1-0");
        var other = AddPending("2-0");
        var alreadyAcknowledged = AddPending("3-0");
        await _redriver.AcknowledgePendingMessages(["3-0"]);
        _valkey.ClearReceivedCalls();

        var result = await _redriver.RedrivePendingMessages(["1-0", "1-0", "3-0", "99-0"]);

        AssertResult(result, 1, 0);
        _pending.Keys.ShouldBe([(RedisValue)"2-0"]);
        await _consumer.Received(1).ConsumeMelding(selected);
        await _consumer.DidNotReceive().ConsumeMelding(other);
        await _consumer.DidNotReceive().ConsumeMelding(alreadyAcknowledged);
        await _valkey
            .Received(1)
            .AcknowledgeMessagesAsync(
                _manifest,
                Arg.Is<IEnumerable<RedisValue>>(ids =>
                    ids.SequenceEqual(new RedisValue[] { "1-0" })
                )
            );
    }

    [Fact]
    public async Task RedriveSelected_WithUnknownIds_DoesNotConsumeOrAcknowledge()
    {
        AddPending("1-0");

        var result = await _redriver.RedrivePendingMessages(["99-0"]);

        AssertResult(result, 0, 0);
        _pending.Count.ShouldBe(1);
        await AssertNoProcessing();
    }

    [Fact]
    public async Task RedriveSelected_WithEmptyCollection_DoesNotRedriveAll()
    {
        AddPending("1-0");

        var result = await _redriver.RedrivePendingMessages([]);

        AssertResult(result, 0, 0);
        _pending.Count.ShouldBe(1);
        await _valkey.DidNotReceive().GetPendingMessagesAsync(Arg.Any<ConsumerManifest>());
        await AssertNoProcessing();
    }

    [Fact]
    public async Task RedriveSelected_WhenConsumerFails_ReportsFailureAndLeavesMessagePending()
    {
        var failing = AddPending("1-0");
        var successful = AddPending("2-0");
        var unselected = AddPending("3-0");
        _consumer
            .ConsumeMelding(failing)
            .Returns(Task.FromException(new InvalidOperationException("Consumer failed.")));

        var result = await _redriver.RedrivePendingMessages(["1-0", "2-0"]);

        AssertResult(result, 1, 1);
        var error = result.RedriveExceptions.ShouldHaveSingleItem();
        error.ValkeyMessageId.ShouldBe("1-0");
        error.MeldingId.ShouldBe(failing.Id);
        error.ExceptionMessage.ShouldBe("Consumer failed.");
        _pending.Keys.OrderBy(id => id.ToString()).ShouldBe(new RedisValue[] { "1-0", "3-0" });
        await _consumer.Received(1).ConsumeMelding(successful);
        await _consumer.DidNotReceive().ConsumeMelding(unselected);
    }

    [Fact]
    public async Task RedriveAll_ConsumesAndAcknowledgesAllPendingMessages()
    {
        var first = AddPending("1-0");
        var second = AddPending("2-0");

        var result = await _redriver.RedrivePendingMessages();

        AssertResult(result, 2, 0);
        _pending.ShouldBeEmpty();
        await _consumer.Received(1).ConsumeMelding(first);
        await _consumer.Received(1).ConsumeMelding(second);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Redrive_WithNoRegistrations_DoesNotFetchOrProcessMessages(bool selected)
    {
        _consumer.ConsumerManifest.Returns(_manifest with { AppRegistrations = [] });

        var result = selected
            ? await _redriver.RedrivePendingMessages(["1-0"])
            : await _redriver.RedrivePendingMessages();

        AssertResult(result, 0, 0);
        await _valkey.DidNotReceive().GetPendingMessagesAsync(Arg.Any<ConsumerManifest>());
        await AssertNoProcessing();
    }

    private Melding AddPending(RedisValue id)
    {
        var melding = new Melding
        {
            Id = Guid.NewGuid(),
            ApplicationId = "test-app",
            Source = MessageSource.Altinn,
            ReceivedAt = DateTime.UtcNow,
        };
        _pending.Add(id, melding);
        _client.GetMelding(melding.Id).Returns(melding);
        return melding;
    }

    private static void AssertResult(RedriveResult result, int successful, int unsuccessful)
    {
        result.SuccessfulCount.ShouldBe(successful);
        result.UnsuccessfulCount.ShouldBe(unsuccessful);
        result.RedriveExceptions.Count.ShouldBe(unsuccessful);
    }

    private async Task AssertNoProcessing()
    {
        await _consumer.DidNotReceive().ConsumeMelding(Arg.Any<Melding>());
        await _client.DidNotReceive().GetMelding(Arg.Any<Guid>());
        await _valkey
            .DidNotReceive()
            .AcknowledgeMessagesAsync(
                Arg.Any<ConsumerManifest>(),
                Arg.Any<IEnumerable<RedisValue>>()
            );
    }

    public void Dispose() => _services.Dispose();
}
