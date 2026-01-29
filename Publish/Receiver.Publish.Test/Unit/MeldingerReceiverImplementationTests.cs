using System.Text.Json;
using Arbeidstilsynet.Meldinger.Receiver.Adapters.Test.fixtures;
using Arbeidstilsynet.Meldinger.Receiver.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Shouldly;
using StackExchange.Redis;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.Meldinger.Receiver.Test;

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

    [Fact]
    public async Task GetNotifications_WhenCalledWithNotificationForApp_ReturnsNotificationForFirstCall()
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
}
