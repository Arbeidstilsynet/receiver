using Arbeidstilsynet.Common.Altinn.Extensions;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Ports.Clients;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Altinn;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.Altinn;

public class AltinnCompletionActionTests
{
    private readonly IAltinnAppsClient _altinnStorageClient = Substitute.For<IAltinnAppsClient>();

    private readonly AltinnCompletionAction _sut;

    public AltinnCompletionActionTests()
    {
        _sut = new AltinnCompletionAction(_altinnStorageClient);
    }

    private Melding SampleMelding(AltinnMetadata altinnMetadata)
    {
        return new Melding()
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Altinn,
            ApplicationId = "altinn-app",
            ContentId = Guid.NewGuid(),
            ReceivedAt = DateTime.Now,
            Tags = altinnMetadata.ToMetadataDictionary(),
            InternalTags = new Dictionary<string, string> { { "tag1", "tag2" } },
            AttachmentIds = [Guid.NewGuid(), Guid.NewGuid()],
        };
    }

    [Fact]
    public async Task RunPostActionFor_WhenCalledWithValidAltinnMelding_SendsCompletionRequest()
    {
        //arrange
        _altinnStorageClient.ClearReceivedCalls();
        var altinnMetadata = new AltinnMetadata
        {
            App = "test",
            Org = "dat",
            InstanceGuid = Guid.NewGuid(),
            InstanceOwnerPartyId = "123123",
        };
        var melding = SampleMelding(altinnMetadata);
        //act
        await _sut.RunPostActionFor(melding);
        //assert
        await _altinnStorageClient
            .Received(1)
            .CompleteInstance(
                "altinn-app",
                Arg.Is<InstanceRequest>(request =>
                    request.InstanceGuid == altinnMetadata.InstanceGuid
                    && request.InstanceOwnerPartyId == altinnMetadata.InstanceOwnerPartyId
                )
            );
    }

    //this should not stop the workflow because we have all the data needed to run it manually when the service is available again
    [Fact]
    public async Task RunPostActionFor_WhenCalledWithValidAltinnMeldingButServiceNotAvailable_ThrowsException()
    {
        //arrange
        _altinnStorageClient.ClearReceivedCalls();
        _altinnStorageClient
            .CompleteInstance("altinn-app", Arg.Any<InstanceRequest>())
            .ThrowsAsync<HttpRequestException>();
        var altinnMetadata = new AltinnMetadata
        {
            App = "test",
            Org = "dat",
            InstanceGuid = Guid.NewGuid(),
            InstanceOwnerPartyId = "123123",
        };
        var melding = SampleMelding(altinnMetadata);
        //act
        var act = () => _sut.RunPostActionFor(melding);
        //assert
        await act.ShouldThrowAsync<HttpRequestException>();
        await _altinnStorageClient
            .Received(1)
            .CompleteInstance(
                "altinn-app",
                Arg.Is<InstanceRequest>(request =>
                    request.InstanceGuid == altinnMetadata.InstanceGuid
                    && request.InstanceOwnerPartyId == altinnMetadata.InstanceOwnerPartyId
                )
            );
    }

    [Fact]
    public async Task RunPostActionFor_WhenCalledWithInvalidAltinnMelding_ShouldThrow()
    {
        //arrange
        var melding = SampleMelding(new AltinnMetadata());
        //act
        var act = () => _sut.RunPostActionFor(melding);
        //assert
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RunPostActionFor_WhenCalledWithNonAltinnMelding_ShouldNotThrow()
    {
        //arrange
        var altinnMelding = SampleMelding(new AltinnMetadata());
        var nonAltinnMelding = altinnMelding with { Source = MessageSource.Api };
        //act
        var act = () => _sut.RunPostActionFor(nonAltinnMelding);
        //assert
        await act.ShouldNotThrowAsync();
        await _altinnStorageClient
            .DidNotReceive()
            .CompleteInstance("altinn-app", Arg.Any<InstanceRequest>());
    }
}
