using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test;

public class MeldingExtensionTests
{
    [Fact]
    public void AddTag_OnMelding_AddsTagCamelCase()
    {
        //arrange
        var melding = new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Api,
            ApplicationId = "test",
            ReceivedAt = DateTime.Now,
            Tags = [],
        };
        //act
        melding.AddTag((Melding m) => m.ReceivedAt, melding.ReceivedAt.ToString());

        //assert
        melding.Tags.Keys.ShouldContain("receivedAt");
    }

    [Fact]
    public void GetTag_OnMelding_ReturnsCorrectTagValue()
    {
        //arrange
        var melding = new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Api,
            ApplicationId = "test",
            ReceivedAt = DateTime.Now,
            Tags = [],
        };
        //act
        melding.AddTag((Melding m) => m.Id, melding.Id.ToString());

        //assert
        melding.GetTag((Melding m) => m.Id).ShouldBe(melding.Id.ToString());
    }

    [Fact]
    public void AddInternalTag_OnMelding_AddsTagCamelCase()
    {
        //arrange
        var melding = new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Api,
            ApplicationId = "test",
            ReceivedAt = DateTime.Now,
            Tags = [],
        };
        //act
        melding.AddInternalTag((Melding m) => m.ReceivedAt, melding.ReceivedAt.ToString());

        //assert
        melding.InternalTags.Keys.ShouldContain("receivedAt");
    }

    [Fact]
    public void GetInternalTag_OnMelding_ReturnsCorrectTagValue()
    {
        //arrange
        var melding = new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Api,
            ApplicationId = "test",
            ReceivedAt = DateTime.Now,
            Tags = [],
        };
        //act
        melding.AddInternalTag((Melding m) => m.Id, melding.Id.ToString());

        //assert
        melding.GetInternalTag((Melding m) => m.Id).ShouldBe(melding.Id.ToString());
    }
}
