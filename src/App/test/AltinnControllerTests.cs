using System.Diagnostics.Metrics;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Ports.Adapter;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Arbeidstilsynet.MeldingerReceiver.App.Test.fixture;
using Arbeidstilsynet.MeldingerReceiver.App.WebApi;
using Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using AltinnFileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test;

public class AltinnControllerTests
{
    private readonly IAltinnRecoveryService _altinnRecoveryService =
        Substitute.For<IAltinnRecoveryService>();
    private readonly IAltinnStorageAdapter _altinnStorageAdapter =
        Substitute.For<IAltinnStorageAdapter>();
    private readonly IAltinnRegistrationService _altinnRegistrationService =
        Substitute.For<IAltinnRegistrationService>();
    private readonly IMeldingService _meldingService = Substitute.For<IMeldingService>();
    private readonly ISubscriptionService _subscriptionService =
        Substitute.For<ISubscriptionService>();

    private readonly AltinnController _sut;

    public AltinnControllerTests()
    {
        var meterFactory = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider()
            .GetRequiredService<IMeterFactory>();

        _sut = new AltinnController(
            _altinnRecoveryService,
            _altinnStorageAdapter,
            _altinnRegistrationService,
            _meldingService,
            _subscriptionService,
            new ApiMeters(meterFactory),
            Substitute.For<ILogger<AltinnController>>()
        );
    }

    [Fact]
    public async Task ProcessInstance_WhenInstanceCannotBeFound_ReturnsNotFound()
    {
        // arrange
        var appId = ApplicationFixture.KnownApplicationId;
        var instanceGuid = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        _altinnRecoveryService.GetNonCompletedInstancesByAppId(appId).Returns([]);
        _altinnStorageAdapter
            .GetInstance(instanceGuid, cancellationToken)
            .Returns((AltinnInstance?)null);

        // act
        var result = await _sut.ProcessInstance(appId, instanceGuid, cancellationToken);

        // assert
        result.Result.ShouldBeOfType<NotFoundResult>();
        await _meldingService
            .DidNotReceive()
            .ProcessMelding(Arg.Any<CreateMeldingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessInstance_WhenInstanceExistsButIsNotReady_ReturnsBadRequest()
    {
        // arrange
        var appId = ApplicationFixture.KnownApplicationId;
        var instanceGuid = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        _altinnRecoveryService.GetNonCompletedInstancesByAppId(appId).Returns([]);
        _altinnStorageAdapter
            .GetInstance(instanceGuid, cancellationToken)
            .Returns(new AltinnInstance { Id = $"1337/{instanceGuid}" });

        // act
        var result = await _sut.ProcessInstance(appId, instanceGuid, cancellationToken);

        // assert
        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBe(
            $"Instance '{instanceGuid}' for appId '{appId}' is not ready to be processed."
        );
        await _meldingService
            .DidNotReceive()
            .ProcessMelding(Arg.Any<CreateMeldingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessInstance_WhenInstanceIsReady_ProcessesAndReturnsMeldingId()
    {
        // arrange
        var appId = ApplicationFixture.KnownApplicationId;
        var instanceGuid = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        var processableInstance = CreateAltinnSummary(appId, instanceGuid);
        var persistedMelding = new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Altinn,
            ApplicationId = appId,
            ReceivedAt = DateTime.UtcNow,
            MainContentId = Guid.NewGuid(),
        };

        _altinnRecoveryService
            .GetNonCompletedInstancesByAppId(appId)
            .Returns([processableInstance]);
        _meldingService
            .ProcessMelding(Arg.Any<CreateMeldingRequest>(), cancellationToken)
            .Returns(persistedMelding);

        // act
        var result = await _sut.ProcessInstance(appId, instanceGuid, cancellationToken);

        // assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<PostMeldingResponse>();
        response.MeldingId.ShouldBe(persistedMelding.Id);
        await _meldingService
            .Received(1)
            .ProcessMelding(
                Arg.Is<CreateMeldingRequest>(request =>
                    request.MeldingId == instanceGuid
                    && request.ApplicationReference == appId
                    && request.Source == MessageSource.Altinn
                ),
                cancellationToken
            );
        await _altinnStorageAdapter.DidNotReceive().GetInstance(instanceGuid, cancellationToken);
    }

    [Fact]
    public async Task DownloadInstanceDataElement_WhenContentTypeIsMissing_UsesOctetStream()
    {
        // arrange
        var instanceGuid = Guid.NewGuid();
        var dataElementId = Guid.NewGuid();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = new MemoryStream([1, 2, 3]);
        _altinnStorageAdapter
            .GetDataElement(instanceGuid, dataElementId, cancellationToken)
            .Returns(new DataElement { Id = dataElementId.ToString(), Filename = "document.bin" });
        _altinnStorageAdapter
            .GetDataElementContent(instanceGuid, dataElementId, cancellationToken)
            .Returns(content);

        // act
        var result = await _sut.DownloadInstanceDataElement(
            instanceGuid,
            dataElementId,
            cancellationToken
        );

        // assert
        var fileResult = result.ShouldBeOfType<FileStreamResult>();
        fileResult.ContentType.ShouldBe("application/octet-stream");
        fileResult.FileDownloadName.ShouldBe("document.bin");
        fileResult.FileStream.ShouldBeSameAs(content);
    }

    private static AltinnInstanceSummary CreateAltinnSummary(string appId, Guid instanceGuid)
    {
        return new AltinnInstanceSummary
        {
            Metadata = new AltinnMetadata
            {
                App = appId,
                Org = "dat",
                InstanceGuid = instanceGuid,
                InstanceOwnerPartyId = "123123",
                DataValues = [],
            },
            SkjemaAsPdf = new AltinnDocument
            {
                DocumentContent = new MemoryStream("main-content"u8.ToArray()),
                FileMetadata = new AltinnFileMetadata
                {
                    AltinnId = Guid.NewGuid(),
                    ContentType = "application/pdf",
                    Filename = "main.pdf",
                    FileScanResult = FileScanResult.Clean,
                },
            },
            StructuredData = new AltinnDocument
            {
                DocumentContent = new MemoryStream("{\"key\":\"value\"}"u8.ToArray()),
                FileMetadata = new AltinnFileMetadata
                {
                    AltinnId = Guid.NewGuid(),
                    ContentType = "application/json",
                    Filename = "structured-data.json",
                    FileScanResult = FileScanResult.Clean,
                },
            },
            Attachments =
            [
                new AltinnDocument
                {
                    DocumentContent = new MemoryStream("attachment"u8.ToArray()),
                    FileMetadata = new AltinnFileMetadata
                    {
                        AltinnId = Guid.NewGuid(),
                        ContentType = "application/pdf",
                        Filename = "attachment.pdf",
                        FileScanResult = FileScanResult.Clean,
                    },
                },
            ],
        };
    }
}
