using Arbeidstilsynet.Meldinger.Receiver.Implementation;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Shouldly;

namespace Arbeidstilsynet.Meldinger.Receiver.Test;

public class ReceiverListenerTests
{
    [Fact]
    public void CheckIfConsumerManifestIsValid_WithEmptyAppRegistrations_ThrowsException()
    {
        //arrange
        var manifest = new ConsumerManifest
        {
            ConsumerName = "manifest-without-registrations",
            AppRegistrations = [],
        };
        //act
        var act = () => ReceiverListener.CheckIfConsumerManifestIsValid(manifest);

        //assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void CheckIfConsumerManifestIsValid_WithInvalidAltinnApp_ThrowsException()
    {
        //arrange
        var manifest = new ConsumerManifest
        {
            ConsumerName = "manifest-without-registrations",
            AppRegistrations =
            [
                new AppRegistration { AppId = "dat/test", MessageSource = MessageSource.Altinn },
            ],
        };
        //act
        var act = () => ReceiverListener.CheckIfConsumerManifestIsValid(manifest);

        //assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void CheckIfConsumerManifestIsValid_WithValidAltinnApp_DoesNotThrowException()
    {
        //arrange
        var manifest = new ConsumerManifest
        {
            ConsumerName = "manifest-without-registrations",
            AppRegistrations =
            [
                new AppRegistration { AppId = "test", MessageSource = MessageSource.Altinn },
            ],
        };
        //act
        var act = () => ReceiverListener.CheckIfConsumerManifestIsValid(manifest);

        //assert
        act.ShouldNotThrow();
    }
}
