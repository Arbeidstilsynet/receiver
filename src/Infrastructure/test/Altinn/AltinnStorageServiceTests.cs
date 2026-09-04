using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Ports.Clients;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Altinn;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Altinn;

public class AltinnStorageServiceTests
{
    private readonly IAltinnStorageClient _client = Substitute.For<IAltinnStorageClient>();
    private readonly AltinnStorageService _sut;

    public AltinnStorageServiceTests()
    {
        _sut = new AltinnStorageService(_client);
    }

    [Fact]
    public async Task GetInstance_WhenCalled_GetsInstanceFromStorageClient()
    {
        // arrange
        var instanceId = Guid.NewGuid();
        var instance = CreateInstance(instanceId);
        var cancellationToken = TestContext.Current.CancellationToken;
        _client.GetInstance(instanceId, cancellationToken).Returns(instance);

        // act
        var result = await _sut.GetInstance(instanceId, cancellationToken);

        // assert
        result.ShouldBe(instance);
        await _client.Received(1).GetInstance(instanceId, cancellationToken);
    }

    [Fact]
    public async Task GetDataElements_WhenInstanceExists_ReturnsDataElements()
    {
        // arrange
        var instanceId = Guid.NewGuid();
        var dataElements = new List<DataElement>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ContentType = "application/pdf",
                Filename = "doc.pdf",
            },
        };
        _client
            .GetInstance(instanceId, TestContext.Current.CancellationToken)
            .Returns(CreateInstance(instanceId, dataElements));

        // act
        var result = await _sut.GetDataElements(instanceId, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBe(dataElements);
    }

    [Fact]
    public async Task GetDataElements_WhenInstanceDoesNotExist_ReturnsNull()
    {
        // arrange
        var instanceId = Guid.NewGuid();
        _client
            .GetInstance(instanceId, TestContext.Current.CancellationToken)
            .Returns((AltinnInstance?)null);

        // act
        var result = await _sut.GetDataElements(instanceId, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDataElement_WhenDataElementExists_ReturnsDataElement()
    {
        // arrange
        var instanceId = Guid.NewGuid();
        var dataElementId = Guid.NewGuid();
        var dataElement = new DataElement
        {
            Id = dataElementId.ToString(),
            ContentType = "application/xml",
            Filename = "payload.xml",
        };
        _client
            .GetInstance(instanceId, TestContext.Current.CancellationToken)
            .Returns(CreateInstance(instanceId, [dataElement]));

        // act
        var result = await _sut.GetDataElement(
            instanceId,
            dataElementId,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBe(dataElement);
    }

    [Fact]
    public async Task GetDataElement_WhenDataElementDoesNotExist_ReturnsNull()
    {
        // arrange
        var instanceId = Guid.NewGuid();
        _client
            .GetInstance(instanceId, TestContext.Current.CancellationToken)
            .Returns(CreateInstance(instanceId));

        // act
        var result = await _sut.GetDataElement(
            instanceId,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDataElementContent_WhenCalled_DoesNotRequireInstanceOwnerPartyId()
    {
        // arrange
        var instanceId = Guid.NewGuid();
        var dataElementId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var stream = new MemoryStream([1, 2, 3]);
        _client.GetInstanceData(Arg.Any<InstanceDataRequest>(), cancellationToken).Returns(stream);

        // act
        var result = await _sut.GetDataElementContent(instanceId, dataElementId, cancellationToken);

        // assert
        result.ShouldBe(stream);
        await _client
            .Received(1)
            .GetInstanceData(
                Arg.Is<InstanceDataRequest>(request =>
                    request.DataId == dataElementId
                    && request.InstanceRequest != null
                    && request.InstanceRequest.InstanceGuid == instanceId
                    && string.IsNullOrEmpty(request.InstanceRequest.InstanceOwnerPartyId)
                ),
                cancellationToken
            );
        await _client.DidNotReceive().GetInstance(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static AltinnInstance CreateInstance(Guid instanceId, List<DataElement>? dataElements = null)
    {
        return new AltinnInstance { Id = $"1337/{instanceId}", Data = dataElements ?? [] };
    }
}
