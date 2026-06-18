using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.fixtures;
using Argon;
using Bogus;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Db;

public class SubscriptionRepositoryReadOnlyTests
    : TestBed<InfrastructureAdapterReadOnlyTestFixtureWithDb>
{
    private static int SeedSize = 10;
    private ISubscriptionsRepository _subscriptionRepository;

    private readonly VerifySettings _verifySettings = new();

    private static Faker<AltinnSubscriptionEntity> AltinnSubscriptionEntityFaker(
        int Seed,
        Guid parentId
    ) =>
        new Faker<AltinnSubscriptionEntity>()
            .UseSeed(Seed)
            .RuleForType(typeof(Guid), faker => faker.Random.Guid())
            .RuleFor(x => x.SubscriptionEntityId, parentId)
            .RuleFor(x => x.SubscriptionId, faker => faker.Random.Int(min: 0))
            .RuleFor(x => x.AppIdentifier, faker => faker.Random.Word());

    private static Faker<ApiSubscriptionEntity> ApiSubscriptionEntityFaker(
        int Seed,
        Guid parentId
    ) =>
        new Faker<ApiSubscriptionEntity>()
            .UseSeed(Seed)
            .RuleForType(typeof(Guid), faker => faker.Random.Guid())
            .RuleFor(x => x.SubscriptionEntityId, parentId)
            .RuleFor(x => x.AppIdentifier, faker => faker.Random.Word());

    private static readonly Faker<SubscriptionEntity> SubscriptionEntityFaker =
        new Faker<SubscriptionEntity>()
            .UseSeed(1337)
            .RuleForType(typeof(Guid), faker => faker.Random.Guid())
            .RuleFor(x => x.ConsumerName, faker => faker.Random.Word())
            .RuleFor(
                x => x.RegisteredAltinnApps,
                (faker, obj) =>
                    AltinnSubscriptionEntityFaker(obj.Id.GetHashCode(), obj.Id)
                        .Generate(faker.Random.Number(0, 3))
            )
            .RuleFor(
                x => x.RegisteredApiApps,
                (faker, obj) =>
                    ApiSubscriptionEntityFaker(obj.Id.GetHashCode(), obj.Id)
                        .Generate(faker.Random.Number(0, 3))
            );

    public SubscriptionRepositoryReadOnlyTests(
        ITestOutputHelper testOutputHelper,
        InfrastructureAdapterReadOnlyTestFixtureWithDb fixtureWithDb
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

    internal static List<SubscriptionEntity> Seed = SubscriptionEntityFaker.Generate(SeedSize);

    [Fact]
    public async Task GetPersistedSubscription_WhenCalledWithExistingAppId_ReturnsConsumerManifest()
    {
        //arrange
        var existingConsumerName = Seed[0].ConsumerName;
        //act
        var result = await _subscriptionRepository.GetPersistedSubscription(existingConsumerName);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task GetPersistedSubscription_WhenCalledWithNontExistingAppId_ReturnsNull()
    {
        //arrange
        var nonExistingConsumerName = "non-existing-consumer-name";
        //act
        var result = await _subscriptionRepository.GetPersistedSubscription(
            nonExistingConsumerName
        );
        //assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetSubscriptions_WhenCalledWithExistingAppId_ReturnsConsumerManifest()
    {
        //arrange
        //act
        var result = await _subscriptionRepository.GetSubscriptions();
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task GetAllAltinnSubscriptions_WhenCalled_ReturnsCompleteList()
    {
        //arrange
        //act
        var result = await _subscriptionRepository.GetAllActiveAltinnSubscriptions();
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task GetAltinnSubscription_WhenCalledWithExistingKey_ReturnsAllMatchingRegistrations()
    {
        //arrange
        var existingAltinnAppId = Seed.SelectMany(s => s.RegisteredAltinnApps)
            .Select(s => s.AppIdentifier)
            .First();
        //act
        var result = await _subscriptionRepository.GetActiveAltinnSubscription(existingAltinnAppId);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public async Task GetAltinnSubscription_WhenCalledWithNotExistingKey_ReturnsNull()
    {
        //arrange
        var existingAltinnAppId = "not-exisiting-altinn-app";
        //act
        var result = await _subscriptionRepository.GetActiveAltinnSubscription(existingAltinnAppId);
        //assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetActiveAppRegistration_WhenCalledWithExistingAltinnAppRegistration_ReturnsMatch()
    {
        //arrange
        var existingAltinnAppId = Seed.SelectMany(s => s.RegisteredAltinnApps)
            .Select(s => s.AppIdentifier)
            .First();
        //act
        var result = await _subscriptionRepository.GetActiveAppRegistration(
            MessageSource.Altinn,
            existingAltinnAppId
        );
        //assert
        result.ShouldBe(
            new AppRegistration
            {
                AppId = existingAltinnAppId,
                MessageSource = MessageSource.Altinn,
            }
        );
    }

    [Fact]
    public async Task GetActiveAppRegistration_WhenCalledWithNonExistingAltinnAppRegistration_ReturnsNull()
    {
        //arrange
        var nonExistingRegistration = "not-registered-yet";
        //act
        var result = await _subscriptionRepository.GetActiveAppRegistration(
            MessageSource.Altinn,
            nonExistingRegistration
        );
        //assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetActiveAppRegistration_WhenCalledWithExistingApiAppRegistration_ReturnsMatch()
    {
        //arrange
        var existingApiAppId = Seed.SelectMany(s => s.RegisteredApiApps)
            .Select(s => s.AppIdentifier)
            .First();
        //act
        var result = await _subscriptionRepository.GetActiveAppRegistration(
            MessageSource.Api,
            existingApiAppId
        );
        //assert
        result.ShouldBe(
            new AppRegistration { AppId = existingApiAppId, MessageSource = MessageSource.Api }
        );
    }

    [Fact]
    public async Task GetActiveAppRegistration_WhenCalledWithNonExistingApiAppRegistration_ReturnsNull()
    {
        //arrange
        var nonExistingRegistration = "not-registered-yet";
        //act
        var result = await _subscriptionRepository.GetActiveAppRegistration(
            MessageSource.Api,
            nonExistingRegistration
        );
        //assert
        result.ShouldBeNull();
    }
}
