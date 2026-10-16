using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Adapters.Test.fixtures;
using Arbeidstilsynet.Receiver.Implementation;
using Arbeidstilsynet.Receiver.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.Receiver.Test;

public class ValkeyConsumerTests : TestBed<MeldingerReceiverFixture>
{
    private IValkeyConsumer _valkeyConsumer;
    private IDatabase _testDatabase;

    public ValkeyConsumerTests(ITestOutputHelper testOutputHelper, MeldingerReceiverFixture fixture)
        : base(testOutputHelper, fixture)
    {
        _valkeyConsumer = fixture.GetService<IValkeyConsumer>(testOutputHelper)!;
        _testDatabase = fixture.GetService<IConnectionMultiplexer>(testOutputHelper)!.GetDatabase();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task GetNotifications_WhenCalledWithNotificationForApp_ReturnsNotificationForFirstCall(
        int existingMessageCount
    )
    {
        //arrange

        var testAppName = $"test-app-{Guid.NewGuid().ToString("n")[..8]}";
        var consumerManifest = new ConsumerManifest()
        {
            ConsumerName = $"consumer-{Guid.NewGuid().ToString("n")[..8]}",
            AppRegistrations =
            [
                new AppRegistration() { AppId = testAppName, MessageSource = MessageSource.Altinn },
            ],
        };
        for (var i = 0; i < existingMessageCount; i++)
        {
            await _testDatabase.StreamAddAsync(
                IConstants.Stream.StreamName,
                [new(IConstants.Stream.MessageKey, "{}")]
            );
        }
        await CreateConsumerGroupAtStreamEnd(consumerManifest);
        var testDto = new Melding()
        {
            ApplicationId = testAppName,
            Id = Guid.NewGuid(),
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
        };
        await _testDatabase.StreamAddAsync(
            IConstants.Stream.StreamName,
            new NameValueEntry[]
            {
                new(IConstants.Stream.MessageKey, JsonSerializer.Serialize(testDto)),
            }
        );
        //act
        var result = await _valkeyConsumer.GetNotificationsAsync(consumerManifest);
        //assert
        result.ShouldNotBeEmpty();
        result.Values.First().ShouldBeEquivalentTo(testDto);

        var resultAfterNotificationWasAlreadyRetrieved =
            await _valkeyConsumer.GetNotificationsAsync(consumerManifest);
        resultAfterNotificationWasAlreadyRetrieved.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNotifications_WhenCalledWithNoNotificationsForApp_ReturnsEmptyAndIdWasAcknowledged()
    {
        //arrange
        var testAppName = $"test-app-{Guid.NewGuid().ToString("n")[..8]}";
        var consumerManifest = new ConsumerManifest()
        {
            ConsumerName = $"consumer-{Guid.NewGuid().ToString("n")[..8]}",
            AppRegistrations =
            [
                new AppRegistration() { AppId = testAppName, MessageSource = MessageSource.Altinn },
            ],
        };
        await CreateConsumerGroupAtStreamEnd(consumerManifest);
        var testDto = new Melding()
        {
            ApplicationId = "non-existing-app-id",
            Id = Guid.NewGuid(),
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
        };
        await _testDatabase.StreamAddAsync(
            IConstants.Stream.StreamName,
            new NameValueEntry[]
            {
                new(IConstants.Stream.MessageKey, JsonSerializer.Serialize(testDto)),
            }
        );
        //act
        var result = await _valkeyConsumer.GetNotificationsAsync(consumerManifest);
        var pendingMessages = await _valkeyConsumer.GetPendingMessagesAsync(consumerManifest);
        //assert
        result.ShouldBeEmpty();
        pendingMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNotifications_WhenCalledWithNotificationsForAppAndAckAfterwards_MakesNotificationNotAccessibleAnymore()
    {
        //arrange
        var testAppName = $"test-app-{Guid.NewGuid().ToString("n")[..8]}";
        var consumerManifest = new ConsumerManifest()
        {
            ConsumerName = $"consumer-{Guid.NewGuid().ToString("n")[..8]}",
            AppRegistrations =
            [
                new AppRegistration() { AppId = testAppName, MessageSource = MessageSource.Altinn },
            ],
        };
        await CreateConsumerGroupAtStreamEnd(consumerManifest);
        var testDto = new Melding()
        {
            ApplicationId = testAppName,
            Id = Guid.NewGuid(),
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
        };
        await _testDatabase.StreamAddAsync(
            IConstants.Stream.StreamName,
            new NameValueEntry[]
            {
                new(IConstants.Stream.MessageKey, JsonSerializer.Serialize(testDto)),
            }
        );
        //act
        var result = await _valkeyConsumer.GetNotificationsAsync(consumerManifest);
        result.Count.ShouldBe(1);
        var pendingMessages = await _valkeyConsumer.GetPendingMessagesAsync(consumerManifest);
        pendingMessages.Count.ShouldBe(1);
        var ackCount = await _valkeyConsumer.AcknowledgeMessageAsync(
            consumerManifest,
            pendingMessages.First().Key
        );
        var resultAfterAck = await _valkeyConsumer.GetPendingMessagesAsync(consumerManifest);
        //assert
        resultAfterAck.ShouldBeEmpty();
    }

    [Fact]
    public async Task AcknowledgeMessage_WhenCalledForAPendingMessage_ReturnsCorrectCount()
    {
        //arrange
        var testAppName = $"test-app-{Guid.NewGuid().ToString("n")[..8]}";
        var consumerManifest = new ConsumerManifest()
        {
            ConsumerName = $"consumer-{Guid.NewGuid().ToString("n")[..8]}",
            AppRegistrations =
            [
                new AppRegistration() { AppId = testAppName, MessageSource = MessageSource.Altinn },
            ],
        };
        await CreateConsumerGroupAtStreamEnd(consumerManifest);
        var testDto = new Melding()
        {
            ApplicationId = testAppName,
            Id = Guid.NewGuid(),
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
        };
        await _testDatabase.StreamAddAsync(
            IConstants.Stream.StreamName,
            new NameValueEntry[]
            {
                new(IConstants.Stream.MessageKey, JsonSerializer.Serialize(testDto)),
            }
        );
        var result = await _valkeyConsumer.GetNotificationsAsync(consumerManifest);
        var pendingMessages = await _valkeyConsumer.GetPendingMessagesAsync(consumerManifest);
        //act
        var ackCount = await _valkeyConsumer.AcknowledgeMessageAsync(
            consumerManifest,
            pendingMessages.First().Key
        );
        //assert
        ackCount.ShouldBe(1);
    }

    [Fact]
    public async Task AcknowledgeMessage_WhenCalledForANotYetReadMessage_ReturnsCorrectCount()
    {
        //arrange
        var testAppName = $"test-app-{Guid.NewGuid().ToString("n")[..8]}";
        var consumerManifest = new ConsumerManifest()
        {
            ConsumerName = $"consumer-{Guid.NewGuid().ToString("n")[..8]}",
            AppRegistrations =
            [
                new AppRegistration() { AppId = testAppName, MessageSource = MessageSource.Altinn },
            ],
        };
        await CreateConsumerGroupAtStreamEnd(consumerManifest);
        var testDto = new Melding()
        {
            ApplicationId = testAppName,
            Id = Guid.NewGuid(),
            ReceivedAt = DateTime.Now,
            Source = MessageSource.Altinn,
        };
        var messageId = await _testDatabase.StreamAddAsync(
            IConstants.Stream.StreamName,
            new NameValueEntry[]
            {
                new(IConstants.Stream.MessageKey, JsonSerializer.Serialize(testDto)),
            }
        );

        //act
        var ackCount = await _valkeyConsumer.AcknowledgeMessageAsync(consumerManifest, messageId);
        //assert
        ackCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(20, false)]
    [InlineData(21, false)]
    [InlineData(21, true)]
    public async Task Redrive_WithMultiplePendingPages_FindsAndAcknowledgesRequestedMessages(
        int count,
        bool redriveAll
    )
    {
        var manifest = new ConsumerManifest
        {
            ConsumerName = $"consumer-{Guid.NewGuid():n}",
            AppRegistrations =
            [
                new AppRegistration
                {
                    AppId = $"test-app-{Guid.NewGuid():n}",
                    MessageSource = MessageSource.Altinn,
                },
            ],
        };
        await CreateConsumerGroupAtStreamEnd(manifest);
        var pending = new Dictionary<RedisValue, Melding>();
        for (var i = 0; i < count; i++)
        {
            var melding = new Melding
            {
                ApplicationId = manifest.AppRegistrations[0].AppId,
                Id = Guid.NewGuid(),
                ReceivedAt = DateTime.UtcNow,
                Source = MessageSource.Altinn,
            };
            var id = await _testDatabase.StreamAddAsync(
                IConstants.Stream.StreamName,
                [new(IConstants.Stream.MessageKey, JsonSerializer.Serialize(melding))]
            );
            pending.Add(id, melding);
        }
        await _testDatabase.StreamReadGroupAsync(
            IConstants.Stream.StreamName,
            manifest.ConsumerName,
            "consumer_1",
            ">",
            count: count
        );

        var consumer = Substitute.For<IMeldingerConsumer>();
        consumer.ConsumerManifest.Returns(manifest);
        var client = Substitute.For<IMeldingerClient>();
        foreach (var melding in pending.Values)
        {
            client.GetMelding(melding.Id).Returns(melding);
        }
        using var services = new ServiceCollection()
            .AddMetrics()
            .AddSingleton<ApiMeters>()
            .AddScoped<IMeldingerClient>(_ => client)
            .BuildServiceProvider();
        var redriver = new MeldingerRedriver(
            services.GetRequiredService<IServiceScopeFactory>(),
            _valkeyConsumer,
            consumer,
            services.GetRequiredService<ApiMeters>(),
            NullLogger<MeldingerRedriver>.Instance
        );
        var selected = pending.Last();

        var result = redriveAll
            ? await redriver.RedrivePendingMessages()
            : await redriver.RedrivePendingMessages([selected.Key]);

        result.SuccessfulCount.ShouldBe(redriveAll ? count : 1);
        result.UnsuccessfulCount.ShouldBe(0);
        result.RedriveExceptions.ShouldBeEmpty();
        await consumer.Received(1).ConsumeMelding(selected.Value);
        if (!redriveAll)
        {
            await consumer.DidNotReceive().ConsumeMelding(pending.First().Value);
        }
        var expectedRemaining = redriveAll
            ? new Dictionary<RedisValue, Melding>()
            : pending.Where(kvp => kvp.Key != selected.Key).ToDictionary();
        var asyncPending = await _valkeyConsumer.GetPendingMessagesAsync(manifest);
        var syncPending = _valkeyConsumer.GetPendingMessages(manifest);
        asyncPending.Keys.ShouldBe(expectedRemaining.Keys);
        syncPending.Keys.ShouldBe(expectedRemaining.Keys);
        foreach (var (id, melding) in expectedRemaining)
        {
            asyncPending[id].ShouldBeEquivalentTo(melding);
            syncPending[id].ShouldBeEquivalentTo(melding);
        }
        var pendingInfo = await _testDatabase.StreamPendingAsync(
            IConstants.Stream.StreamName,
            manifest.ConsumerName
        );
        pendingInfo.PendingMessageCount.ShouldBe(expectedRemaining.Count);
    }

    private Task CreateConsumerGroupAtStreamEnd(ConsumerManifest manifest) =>
        _testDatabase.StreamCreateConsumerGroupAsync(
            IConstants.Stream.StreamName,
            manifest.ConsumerName,
            "$",
            createStream: true
        );
}
