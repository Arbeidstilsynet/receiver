using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Altinn;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.DependencyInjection;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.Altinn;

public class AltinnRegistrationServiceTests
{
    private IAltinnAdapter _altinnAdapter = Substitute.For<IAltinnAdapter>();
    private ILogger<AltinnRegistrationService> _logger = Substitute.For<
        ILogger<AltinnRegistrationService>
    >();
    private IMapper _mapper = Substitute.For<IMapper>();

    private InfrastructureConfiguration _infraConfigMock =
        Substitute.For<InfrastructureConfiguration>();
    private AltinnRegistrationService _sut;

    private static readonly string DummyCallbackUrl = "http://donothing";

    public AltinnRegistrationServiceTests()
    {
        _sut = new AltinnRegistrationService(
            _altinnAdapter,
            Options.Create(_infraConfigMock),
            _logger,
            _mapper
        );
        _infraConfigMock.AppDomain.Returns(DummyCallbackUrl);
    }

    [Fact]
    public async Task RegisterAltinnApplication_WhenCalledWithNewAppId_CreatesNewAppRegistration()
    {
        //arrange
        _altinnAdapter.ClearReceivedCalls();
        var newAppId = "test";

        //act
        await _sut.RegisterAltinnApplication(newAppId);
        //assert
        await _altinnAdapter
            .Received(1)
            .SubscribeForCompletedProcessEvents(
                Arg.Is<SubscriptionRequestDto>(s =>
                    s.AltinnAppId == newAppId
                    && s.CallbackUrl
                        == new Uri(new Uri(DummyCallbackUrl), "webhook/receive-altinn-cloudevent")
                )
            );
    }

    [Fact]
    public async Task UnsubscribeAltinnApplication_WhenCalledWithNotExistingId_ReturnsFalse()
    {
        //arrange
        _altinnAdapter.ClearReceivedCalls();
        var notExistingId = 123;
        _altinnAdapter
            .GetAltinnSubscription(notExistingId)
            .Returns(Task.FromResult((AltinnSubscription?)null));
        _altinnAdapter
            .UnsubscribeForCompletedProcessEvents(new AltinnSubscription { Id = notExistingId })
            .Returns(false);
        //act
        var result = await _sut.UnsubscribeAltinnApplication(notExistingId);
        //assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UnsubscribeAltinnApplication_WhenCalledWithExistingId_ReturnsTrue()
    {
        //arrange
        _altinnAdapter.ClearReceivedCalls();
        var existingId = 123;
        var subscriptionResponseMock = Substitute.For<AltinnSubscription?>();
        _altinnAdapter.GetAltinnSubscription(existingId).Returns(subscriptionResponseMock);
        _altinnAdapter
            .UnsubscribeForCompletedProcessEvents(subscriptionResponseMock!)
            .Returns(true);
        //act
        var result = await _sut.UnsubscribeAltinnApplication(existingId);
        //assert
        result.ShouldBeTrue();
    }
}
