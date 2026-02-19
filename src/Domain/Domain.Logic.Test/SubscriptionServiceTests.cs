using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test;

public class SubscriptionServiceTests
{
    private readonly ISubscriptionService _sut;
    private readonly ISubscriptionsRepository _subscriptionRepository;
    private readonly IAltinnRegistrationService _altinnRegistrationService;
    private readonly DomainConfiguration _domainConfiguration;

    public SubscriptionServiceTests()
    {
        _subscriptionRepository = Substitute.For<ISubscriptionsRepository>();
        _altinnRegistrationService = Substitute.For<IAltinnRegistrationService>();
        _domainConfiguration = Substitute.For<DomainConfiguration>();

        _sut = new SubscriptionService(
            _altinnRegistrationService,
            _subscriptionRepository,
            Options.Create(_domainConfiguration)
        );

        // Default behavior: Altinn registration succeeds
        _altinnRegistrationService
            .RegisterAltinnApplication(Arg.Any<string>())
            .Returns(Task.FromResult(Substitute.For<AltinnEventsSubscription>()));
    }

    #region Test Data Builders

    private static class TestData
    {
        public static AppRegistration CreateAltinnAppRegistration(
            string appId = "test-altinn-app"
        ) => new AppRegistration { MessageSource = MessageSource.Altinn, AppId = appId };

        public static AppRegistration CreateApiAppRegistration(string appId = "test-api-app") =>
            new AppRegistration { MessageSource = MessageSource.Api, AppId = appId };

        public static ConsumerManifest CreateConsumerManifest(
            string consumerName,
            params AppRegistration[] appRegistrations
        ) =>
            new ConsumerManifest
            {
                ConsumerName = consumerName,
                AppRegistrations = appRegistrations.ToList(),
            };

        public static AltinnConnection CreateAltinnConnection(
            string altinnAppId,
            Guid? internalId = null,
            int? subscriptionId = null
        ) =>
            new AltinnConnection
            {
                AltinnAppId = altinnAppId,
                InternalId = internalId ?? Guid.NewGuid(),
                SubscriptionId = subscriptionId,
            };
    }

    #endregion

    #region ShouldMeldingForAppIdBeIgnored Tests

    [Fact]
    public async Task ShouldMeldingForAppIdBeIgnored_WhenDomainSettingIsDisabled_ReturnsFalse()
    {
        // Arrange
        _domainConfiguration.AllowOnlyRegisteredApps.Returns(false);
        var appRegistration = TestData.CreateAltinnAppRegistration();
        _subscriptionRepository
            .GetActiveAppRegistration(MessageSource.Altinn, "any-app")
            .Returns(appRegistration);

        // Act
        var result = await _sut.ShouldMeldingForAppIdBeIgnored(MessageSource.Altinn, "any-app");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldMeldingForAppIdBeIgnored_WhenAppIsNotRegistered_ReturnsTrue()
    {
        // Arrange
        _domainConfiguration.AllowOnlyRegisteredApps.Returns(true);
        _subscriptionRepository
            .GetActiveAppRegistration(MessageSource.Altinn, "unregistered-app")
            .Returns((AppRegistration?)null);

        // Act
        var result = await _sut.ShouldMeldingForAppIdBeIgnored(
            MessageSource.Altinn,
            "unregistered-app"
        );

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldMeldingForAppIdBeIgnored_WhenAppIsRegistered_ReturnsFalse()
    {
        // Arrange
        _domainConfiguration.AllowOnlyRegisteredApps.Returns(true);
        var appRegistration = TestData.CreateAltinnAppRegistration("registered-app");
        _subscriptionRepository
            .GetActiveAppRegistration(MessageSource.Altinn, "registered-app")
            .Returns(appRegistration);

        // Act
        var result = await _sut.ShouldMeldingForAppIdBeIgnored(
            MessageSource.Altinn,
            "registered-app"
        );

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region CreateSubscription Tests

    [Fact]
    public async Task CreateSubscription_WhenManifestUnchanged_SkipsRegistrationAndPersistence()
    {
        // Arrange
        var manifest = TestData.CreateConsumerManifest(
            "test-consumer",
            TestData.CreateAltinnAppRegistration("app-1"),
            TestData.CreateAltinnAppRegistration("app-2")
        );

        _subscriptionRepository.GetPersistedSubscription("test-consumer").Returns(manifest);

        // Act
        await _sut.CreateSubscription(manifest);

        // Assert - Nothing should happen when manifest is unchanged
        await _altinnRegistrationService
            .DidNotReceive()
            .RegisterAltinnApplication(Arg.Any<string>());
        await _subscriptionRepository
            .DidNotReceive()
            .CreateSubscription(Arg.Any<ConsumerManifest>());
        await _subscriptionRepository
            .DidNotReceive()
            .UpdateAltinnSubscriptionId(Arg.Any<Guid>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CreateSubscription_WhenManifestChanged_DeletesOldAndCreatesNewSubscriptions()
    {
        // Arrange: Original manifest has 2 apps
        var originalApp1 = TestData.CreateAltinnAppRegistration("app-1");
        var originalApp2 = TestData.CreateAltinnAppRegistration("app-2");
        var originalManifest = TestData.CreateConsumerManifest(
            "test-consumer",
            originalApp1,
            originalApp2
        );

        // Changed manifest only has 1 app (app-1)
        var changedManifest = TestData.CreateConsumerManifest("test-consumer", originalApp1);

        // Existing subscriptions in the database (both already registered with Altinn)
        var existingConnection1 = TestData.CreateAltinnConnection(
            originalApp1.AppId,
            subscriptionId: 100
        );
        var existingConnection2 = TestData.CreateAltinnConnection(
            originalApp2.AppId,
            subscriptionId: 200
        );

        // New connection created for app-1 (not yet registered with Altinn)
        var newConnection1 = TestData.CreateAltinnConnection(originalApp1.AppId);

        _subscriptionRepository.GetPersistedSubscription("test-consumer").Returns(originalManifest);
        _subscriptionRepository
            .GetActiveAltinnSubscription(originalApp1.AppId)
            .Returns(existingConnection1);
        _subscriptionRepository
            .GetActiveAltinnSubscription(originalApp2.AppId)
            .Returns(existingConnection2);
        _subscriptionRepository
            .CreateSubscription(changedManifest)
            .Returns(new[] { newConnection1 });

        // Act
        await _sut.CreateSubscription(changedManifest);

        // Assert - Should unsubscribe from both old apps in Altinn
        await _altinnRegistrationService.Received(1).UnsubscribeAltinnApplication(100);
        await _altinnRegistrationService.Received(1).UnsubscribeAltinnApplication(200);

        // Assert - Should delete old subscription from database
        await _subscriptionRepository.Received(1).DeleteSubscription(originalManifest);

        // Assert - Should register only app-1 (the one in the new manifest)
        await _altinnRegistrationService.Received(1).RegisterAltinnApplication(originalApp1.AppId);
        await _altinnRegistrationService
            .DidNotReceive()
            .RegisterAltinnApplication(originalApp2.AppId);

        // Assert - Should create new subscription in database
        await _subscriptionRepository.Received(1).CreateSubscription(changedManifest);

        // Assert - Should update subscription ID for the new connection
        await _subscriptionRepository
            .Received(1)
            .UpdateAltinnSubscriptionId(newConnection1.InternalId, Arg.Any<int>());
    }

    [Fact]
    public async Task CreateSubscription_WithMultipleAltinnApps_RegistersAllAppsWithAltinn()
    {
        // Arrange
        var app1 = TestData.CreateAltinnAppRegistration("app-1");
        var app2 = TestData.CreateAltinnAppRegistration("app-2");
        var manifest = TestData.CreateConsumerManifest("new-consumer", app1, app2);

        // First connection needs Altinn registration (no subscriptionId yet)
        var connection1 = TestData.CreateAltinnConnection(app1.AppId);
        // Second connection already has Altinn subscription
        var connection2 = TestData.CreateAltinnConnection(app2.AppId, subscriptionId: 100);

        _subscriptionRepository
            .GetPersistedSubscription("new-consumer")
            .Returns((ConsumerManifest?)null);
        _subscriptionRepository
            .CreateSubscription(manifest)
            .Returns(new[] { connection1, connection2 });

        // Act
        await _sut.CreateSubscription(manifest);

        // Assert - Both apps should be registered with Altinn
        await _altinnRegistrationService.Received(1).RegisterAltinnApplication(app1.AppId);
        await _altinnRegistrationService.Received(1).RegisterAltinnApplication(app2.AppId);

        // Assert - Subscription created
        await _subscriptionRepository.Received(1).CreateSubscription(manifest);

        // Assert - Both subscription IDs should be updated
        await _subscriptionRepository
            .Received(1)
            .UpdateAltinnSubscriptionId(connection1.InternalId, Arg.Any<int>());
        await _subscriptionRepository
            .Received(1)
            .UpdateAltinnSubscriptionId(connection2.InternalId, Arg.Any<int>());
    }

    [Fact]
    public async Task CreateSubscription_WithApiAppsOnly_PersistsWithoutAltinnRegistration()
    {
        // Arrange
        var apiApp = TestData.CreateApiAppRegistration("api-app");
        var manifest = TestData.CreateConsumerManifest("api-consumer", apiApp);

        _subscriptionRepository
            .GetPersistedSubscription("api-consumer")
            .Returns((ConsumerManifest?)null);
        _subscriptionRepository
            .CreateSubscription(manifest)
            .Returns(Array.Empty<AltinnConnection>());

        // Act
        await _sut.CreateSubscription(manifest);

        // Assert - Should NOT call Altinn service for API-only apps
        await _altinnRegistrationService
            .DidNotReceive()
            .RegisterAltinnApplication(Arg.Any<string>());

        // Assert - Should persist subscription
        await _subscriptionRepository.Received(1).CreateSubscription(manifest);

        // Assert - No Altinn subscription IDs to update
        await _subscriptionRepository
            .DidNotReceive()
            .UpdateAltinnSubscriptionId(Arg.Any<Guid>(), Arg.Any<int>());
    }

    #endregion
}
