using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Altinn;
using Bogus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Altinn;

public class AltinnRecoveryServiceTests
{
    private IAltinnSubscriptionAdapter _altinnAdapter = Substitute.For<IAltinnSubscriptionAdapter>();
    private ISubscriptionsRepository _subscriptionsRepository =
        Substitute.For<ISubscriptionsRepository>();
    private ILogger<AltinnRecoveryService> _logger = Substitute.For<
        ILogger<AltinnRecoveryService>
    >();
    private AltinnRecoveryService _sut;

    private static readonly Faker<AltinnInstanceSummary> AltinnInstanceSummaryFaker =
        new Faker<AltinnInstanceSummary>()
            .UseSeed(1337)
            .RuleForType(typeof(AltinnMetadata), x => Substitute.For<AltinnMetadata>())
            .RuleForType(typeof(AltinnDocument), x => Substitute.For<AltinnDocument>())
            .RuleFor(x => x.Attachments, faker => []);

    private AltinnInstanceSummary[] GetDummyInstances(int count) =>
        [.. AltinnInstanceSummaryFaker.Generate(count)];

    private static readonly AltinnConnection SampleTestAppRegistration = new AltinnConnection
    {
        InternalId = Guid.NewGuid(),
        AltinnAppId = "test",
        SubscriptionId = 1234567,
    };

    private static readonly AltinnConnection SampleTestAppRegistration2 = new AltinnConnection
    {
        InternalId = Guid.NewGuid(),
        AltinnAppId = "test2",
        SubscriptionId = 7654321,
    };

    public AltinnRecoveryServiceTests()
    {
        _sut = new AltinnRecoveryService(_altinnAdapter, _subscriptionsRepository, _logger);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(1, 1, 2)]
    [InlineData(2, 2, 4)]
    public async Task GetAllNonCompletedInstancesForRegisteredApps_WhenCalledWithRegisteredApps_GetsInstanceData(
        int nonCompletedInstancesForFirstAppCount,
        int nonCompletedInstancesForSecondAppCount,
        int expectedResultCount
    )
    {
        //arrange
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration, SampleTestAppRegistration2]);
        _altinnAdapter
            .GetNonCompletedInstances(SampleTestAppRegistration.AltinnAppId, true)
            .Returns(GetDummyInstances(nonCompletedInstancesForFirstAppCount));
        _altinnAdapter
            .GetNonCompletedInstances(SampleTestAppRegistration2.AltinnAppId, true)
            .Returns(GetDummyInstances(nonCompletedInstancesForSecondAppCount));
        //act
        var result = await _sut.GetAllNonCompletedInstancesForRegisteredApps();
        //assert
        result.SelectMany(s => s.Value).Count().ShouldBe(expectedResultCount);
    }

    [Fact]
    public async Task GetAllNonCompletedInstancesForRegisteredApps_WhenCalledWithoutRegisteredApps_ReturnsEmptyList()
    {
        //arrange
        _subscriptionsRepository.GetAllActiveAltinnSubscriptions().Returns([]);
        //act
        var result = await _sut.GetAllNonCompletedInstancesForRegisteredApps();
        //assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAllNonCompletedInstancesForRegisteredApps_WhenOneAppThrows_SkipsThatAppAndContinues()
    {
        // arrange
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration, SampleTestAppRegistration2]);
        _altinnAdapter
            .GetNonCompletedInstances(SampleTestAppRegistration.AltinnAppId, true)
            .Returns<IEnumerable<AltinnInstanceSummary>>(_ =>
                throw new InvalidOperationException("Library threw while processing instances")
            );
        _altinnAdapter
            .GetNonCompletedInstances(SampleTestAppRegistration2.AltinnAppId, true)
            .Returns(GetDummyInstances(2));

        // act
        var result = await _sut.GetAllNonCompletedInstancesForRegisteredApps();

        // assert: failed app is omitted; successful app is returned
        result.Count.ShouldBe(1);
        result.ContainsKey(SampleTestAppRegistration.AltinnAppId).ShouldBeFalse();
        result[SampleTestAppRegistration2.AltinnAppId].Count().ShouldBe(2);
    }
}
