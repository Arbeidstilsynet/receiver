using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Argon;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db;

public class SubscriptionRepositoryWriteTests : TestBed<InfrastructureAdapterWriteTestFixtureWithDb>
{
    private readonly ISubscriptionsRepository _subscriptionRepository;

    private readonly VerifySettings _verifySettings = new();

    public SubscriptionRepositoryWriteTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterWriteTestFixtureWithDb fixtureWithDb
    )
        : base(testOutputHelper, fixtureWithDb)
    {
        _subscriptionRepository = fixtureWithDb.GetService<ISubscriptionsRepository>(
            testOutputHelper
        )!;
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.AddExtraSettings(jsonSettings =>
        {
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
        });
    }

    [Fact]
    public async Task CreateSubscription_WhenCalledWithoutAnyAppRegistrations_ReturnsConsumerManifestWithoutRegistrations()
    {
        //arrange
        var newConsumer = new ConsumerManifest
        {
            ConsumerName = "a-new-dummy-consumer",
            AppRegistrations = [],
        };
        //act
        await _subscriptionRepository.DeleteSubscription(newConsumer);
        try
        {
            await _subscriptionRepository.CreateSubscription(newConsumer);
            //assert
            await Verify(
                await _subscriptionRepository.GetPersistedSubscription(newConsumer.ConsumerName),
                _verifySettings
            );
        }
        finally
        {
            await _subscriptionRepository.DeleteSubscription(newConsumer);
        }
    }

    [Fact]
    public async Task CreateSubscription_WhenCalledWithAppRegistrations_ReturnsConsumerManifestWithRegistrations()
    {
        //arrange
        var newConsumer = new ConsumerManifest
        {
            ConsumerName = "a-new-consumer",
            AppRegistrations =
            [
                new AppRegistration { AppId = "app-1", MessageSource = MessageSource.Altinn },
                new AppRegistration { AppId = "app-2", MessageSource = MessageSource.Api },
            ],
        };
        //act
        await _subscriptionRepository.DeleteSubscription(newConsumer);
        try
        {
            await _subscriptionRepository.CreateSubscription(newConsumer);
            //assert
            await Verify(
                await _subscriptionRepository.GetPersistedSubscription(newConsumer.ConsumerName),
                _verifySettings
            );
        }
        finally
        {
            await _subscriptionRepository.DeleteSubscription(newConsumer);
        }
    }

    [Fact]
    public async Task UpdateAltinnSubscriptionId_WhenCalledWithNewSubscriptionId_UpdatesAltinnEntity()
    {
        //arrange
        var newAltinnSubscriptionId = 123454321;

        var consumerName = $"test-consumer-{Guid.NewGuid():N}";
        var altinnAppId = $"test-altinn-app-{Guid.NewGuid():N}";
        var newConsumer = new ConsumerManifest
        {
            ConsumerName = consumerName,
            AppRegistrations =
            [
                new AppRegistration { AppId = altinnAppId, MessageSource = MessageSource.Altinn },
            ],
        };

        try
        {
            await _subscriptionRepository.CreateSubscription(newConsumer);

            var existing = await _subscriptionRepository.GetActiveAltinnSubscription(altinnAppId);
            existing.ShouldNotBeNull();
            existing.SubscriptionId.ShouldNotBe(newAltinnSubscriptionId);

            //act
            await _subscriptionRepository.UpdateAltinnSubscriptionId(
                existing.InternalId,
                newAltinnSubscriptionId
            );

            //assert
            (
                await _subscriptionRepository.GetActiveAltinnSubscription(altinnAppId)
            )?.SubscriptionId.ShouldBe(newAltinnSubscriptionId);
        }
        finally
        {
            await _subscriptionRepository.DeleteSubscription(newConsumer);
        }
    }

    [Fact]
    public async Task DeleteSubscription_WhenCalledWithExistingConsumerManifest_DeletesAll()
    {
        //arrange

        var consumerName = $"test-consumer-{Guid.NewGuid():N}";
        var altinnAppId = $"test-altinn-app-{Guid.NewGuid():N}";
        var apiAppId = $"test-api-app-{Guid.NewGuid():N}";
        var newConsumer = new ConsumerManifest
        {
            ConsumerName = consumerName,
            AppRegistrations =
            [
                new AppRegistration { AppId = altinnAppId, MessageSource = MessageSource.Altinn },
                new AppRegistration { AppId = apiAppId, MessageSource = MessageSource.Api },
            ],
        };

        await _subscriptionRepository.CreateSubscription(newConsumer);

        var persistedBeforeDelete = await _subscriptionRepository.GetPersistedSubscription(
            newConsumer.ConsumerName
        );
        persistedBeforeDelete.ShouldNotBeNull();

        var activeAltinnBeforeDelete = await _subscriptionRepository.GetActiveAltinnSubscription(
            altinnAppId
        );
        activeAltinnBeforeDelete.ShouldNotBeNull();
        //act
        await _subscriptionRepository.DeleteSubscription(newConsumer);
        //assert
        var persistedAfterDelete = await _subscriptionRepository.GetPersistedSubscription(
            newConsumer.ConsumerName
        );
        persistedAfterDelete.ShouldBeNull();

        var activeAltinnAfterDelete = await _subscriptionRepository.GetActiveAltinnSubscription(
            altinnAppId
        );
        activeAltinnAfterDelete.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteSubscription_WhenCalledWithNonExistingConsumerManifest_DoesNothing()
    {
        //arrange

        //act
        var act = () =>
            _subscriptionRepository.DeleteSubscription(
                new ConsumerManifest { ConsumerName = "not-existing-consumer" }
            );
        //assert
        await act.ShouldNotThrowAsync();
    }
}
