using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Request;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.Common.Altinn.Ports.Clients;
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
    private IAltinnAdapter _altinnAdapter = Substitute.For<IAltinnAdapter>();
    private IAltinnStorageClient _altinnStorageClient = Substitute.For<IAltinnStorageClient>();
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

    private static readonly AltinnConnection SampleTestAppRegistration3 = new AltinnConnection
    {
        InternalId = Guid.NewGuid(),
        AltinnAppId = "test3",
        SubscriptionId = 1111111,
    };

    private static readonly AltinnConnection SampleQualifiedAppRegistration = new AltinnConnection
    {
        InternalId = Guid.NewGuid(),
        AltinnAppId = "dat/qualified",
        SubscriptionId = 2222222,
    };

    public AltinnRecoveryServiceTests()
    {
        _sut = new AltinnRecoveryService(
            _altinnAdapter,
            _altinnStorageClient,
            _subscriptionsRepository,
            _logger
        );
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
    public async Task GetInstanceMetadata_WhenInstanceExistsOnFirstPage_ReturnsInstance()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var instance = CreateInstance(instanceGuid, "1001");
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == $"dat/{SampleTestAppRegistration.AltinnAppId}"
                    && string.IsNullOrEmpty(q.ContinuationToken)
                )
            )
            .Returns(CreatePage([instance], null));

        // act
        var result = await _sut.GetInstanceMetadata(
            instanceGuid,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(instance.Id);
    }

    [Fact]
    public async Task GetInstanceMetadata_WhenInstanceExistsOnSecondPage_UsesContinuationToken()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var instance = CreateInstance(instanceGuid, "1002");
        const string continuationToken = "token-123";
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == $"dat/{SampleTestAppRegistration.AltinnAppId}"
                    && string.IsNullOrEmpty(q.ContinuationToken)
                )
            )
            .Returns(
                CreatePage(
                    [CreateInstance(Guid.NewGuid(), "9999")],
                    $"https://example.test?continuationToken={continuationToken}&size=100"
                )
            );
        _altinnStorageClient
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == $"dat/{SampleTestAppRegistration.AltinnAppId}"
                    && q.ContinuationToken == continuationToken
                )
            )
            .Returns(CreatePage([instance], null));

        // act
        var result = await _sut.GetInstanceMetadata(
            instanceGuid,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(instance.Id);
    }

    [Fact]
    public async Task GetInstanceMetadata_WhenNoMatchingInstanceAcrossApps_ReturnsNull()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration, SampleTestAppRegistration2]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(
                CreatePage([CreateInstance(Guid.NewGuid(), "1111")], null),
                CreatePage([CreateInstance(Guid.NewGuid(), "2222")], null)
            );

        // act
        var result = await _sut.GetInstanceMetadata(
            instanceGuid,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDataElementsForInstance_WhenInstanceExists_ReturnsDataElements()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var dataElements = new List<DataElement>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ContentType = "application/pdf",
                Filename = "doc.pdf",
            },
        };
        var instance = CreateInstance(instanceGuid, "1003", dataElements);
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(CreatePage([instance], null));

        // act
        var result = await _sut.GetDataElementsForInstance(
            instanceGuid,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Filename.ShouldBe("doc.pdf");
    }

    [Fact]
    public async Task GetDataElementsForInstance_WhenInstanceDoesNotExist_ReturnsNull()
    {
        // arrange
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(CreatePage([], null));

        // act
        var result = await _sut.GetDataElementsForInstance(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadDataElement_WhenDataElementExists_ReturnsDownload()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var dataElementId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);
        var instance = CreateInstance(
            instanceGuid,
            "1004",
            [
                new DataElement
                {
                    Id = dataElementId.ToString(),
                    ContentType = "application/xml",
                    Filename = "payload.xml",
                },
            ]
        );
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(CreatePage([instance], null));
        _altinnStorageClient.GetInstanceData(Arg.Any<InstanceDataRequest>()).Returns(stream);

        // act
        var result = await _sut.DownloadDataElement(
            instanceGuid,
            dataElementId,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldNotBeNull();
        result.ContentType.ShouldBe("application/xml");
        result.Filename.ShouldBe("payload.xml");
        result.Content.ShouldBe(stream);
        await _altinnStorageClient
            .Received(1)
            .GetInstanceData(
                Arg.Is<InstanceDataRequest>(request =>
                    request.DataId == dataElementId
                    && request.InstanceRequest.InstanceGuid == instanceGuid
                    && request.InstanceRequest.InstanceOwnerPartyId == "1004"
                )
            );
    }

    [Fact]
    public async Task DownloadDataElement_WhenDataElementDoesNotExist_ReturnsNull()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(
                CreatePage(
                    [
                        CreateInstance(
                            instanceGuid,
                            "1005",
                            [new DataElement { Id = Guid.NewGuid().ToString() }]
                        ),
                    ],
                    null
                )
            );

        // act
        var result = await _sut.DownloadDataElement(
            instanceGuid,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
        await _altinnStorageClient.DidNotReceive().GetInstanceData(Arg.Any<InstanceDataRequest>());
    }

    [Fact]
    public async Task DownloadDataElement_WhenInstanceOwnerPartyIdMissing_ReturnsNull()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var instance = CreateInstance(instanceGuid, null);
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(CreatePage([instance], null));

        // act
        var result = await _sut.DownloadDataElement(
            instanceGuid,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
        await _altinnStorageClient.DidNotReceive().GetInstanceData(Arg.Any<InstanceDataRequest>());
    }

    [Fact]
    public async Task GetInstanceMetadata_WhenNextHasNoContinuationToken_DoesNotRequestAnotherPage()
    {
        // arrange
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleTestAppRegistration]);
        _altinnStorageClient
            .GetInstances(Arg.Any<InstanceQueryParameters>())
            .Returns(CreatePage([], "https://example.test?size=100"));

        // act
        var result = await _sut.GetInstanceMetadata(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
        await _altinnStorageClient.Received(1).GetInstances(Arg.Any<InstanceQueryParameters>());
    }

    [Fact]
    public async Task GetInstanceMetadata_WhenDuplicateAppRegistrations_QueriesEachAppOnlyOnce()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var instance = CreateInstance(instanceGuid, "1006");
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([
                SampleTestAppRegistration,
                SampleTestAppRegistration,
                SampleTestAppRegistration3,
            ]);
        _altinnStorageClient
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == $"dat/{SampleTestAppRegistration.AltinnAppId}"
                )
            )
            .Returns(CreatePage([instance], null));
        _altinnStorageClient
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == $"dat/{SampleTestAppRegistration3.AltinnAppId}"
                )
            )
            .Returns(CreatePage([], null));

        // act
        var result = await _sut.GetInstanceMetadata(
            instanceGuid,
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldNotBeNull();
        await _altinnStorageClient
            .Received(1)
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == $"dat/{SampleTestAppRegistration.AltinnAppId}"
                )
            );
    }

    [Fact]
    public async Task GetInstanceMetadata_WhenAppIdIsAlreadyQualified_DoesNotPrefixAgain()
    {
        // arrange
        _subscriptionsRepository
            .GetAllActiveAltinnSubscriptions()
            .Returns([SampleQualifiedAppRegistration]);
        _altinnStorageClient
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == SampleQualifiedAppRegistration.AltinnAppId
                )
            )
            .Returns(CreatePage([], null));

        // act
        var result = await _sut.GetInstanceMetadata(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // assert
        result.ShouldBeNull();
        await _altinnStorageClient
            .Received(1)
            .GetInstances(
                Arg.Is<InstanceQueryParameters>(q =>
                    q.AppId == SampleQualifiedAppRegistration.AltinnAppId
                )
            );
    }

    private static AltinnQueryResponse<AltinnInstance> CreatePage(
        List<AltinnInstance> instances,
        string? next
    )
    {
        return new AltinnQueryResponse<AltinnInstance>
        {
            Instances = instances,
            Next = next ?? string.Empty,
        };
    }

    private static AltinnInstance CreateInstance(
        Guid instanceGuid,
        string? ownerPartyId,
        List<DataElement>? dataElements = null
    )
    {
        return new AltinnInstance
        {
            Id = $"1337/{instanceGuid}",
            InstanceOwner = new InstanceOwner { PartyId = ownerPartyId ?? string.Empty },
            Data = dataElements ?? [],
        };
    }
}
