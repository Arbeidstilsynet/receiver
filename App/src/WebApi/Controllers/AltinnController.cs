using System.Net;
using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.App.Jobs;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AltinnController(
    IAltinnRecoveryService altinnRecoveryService,
    IAltinnRegistrationService altinnRegistrationService,
    IMeldingService meldingService,
    ISubscriptionService subscriptionService,
    ApiMeters apiMeters,
    ILogger<AltinnController> logger
) : ControllerBase
{
    [HttpGet("non-completed-instances")]
    public async Task<ActionResult<NonCompletedInstancesResult[]>> GetAllNonCompletedInstances()
    {
        return Ok(
            (await altinnRecoveryService.GetMetadataForAllNonCompletedInstancesForRegisteredApps())
                .Select(s => new NonCompletedInstancesResult
                {
                    AppId = s.Key,
                    NonCompletedInstances = s.Value,
                })
                .ToArray()
        );
    }

    [HttpGet("instances/not-process-complete")]
    public async Task<
        ActionResult<NotProcessCompleteInstancesResult[]>
    > GetAllNotProcessCompleteInstances()
    {
        var instancesByApp =
            await altinnRecoveryService.GetAllNonCompletedInstancesForRegisteredApps();

        return Ok(
            instancesByApp
                .Select(appInstances => new NotProcessCompleteInstancesResult
                {
                    AppId = appInstances.Key,
                    Instances = appInstances
                        .Value.Select(instance => instance.ToResponse(Url, appInstances.Key))
                        .ToList(),
                })
                .ToArray()
        );
    }

    [HttpGet("non-completed-instances/{appId}")]
    public async Task<ActionResult<IEnumerable<AltinnMetadata>>> GetAllNonCompletedInstances(
        [FromRoute] string appId
    )
    {
        return Ok(await altinnRecoveryService.GetMetadataForNonCompletedInstancesByAppId(appId));
    }

    [HttpGet("instances/not-process-complete/{appId}")]
    public async Task<
        ActionResult<IReadOnlyList<AltinnInstanceSummaryResponse>>
    > GetNotProcessCompleteInstances([FromRoute] string appId)
    {
        var instances = await altinnRecoveryService.GetNonCompletedInstancesByAppId(appId);
        if (instances == null)
            return NotFound();

        return Ok(instances.Select(instance => instance.ToResponse(Url, appId)).ToList());
    }

    [HttpGet("instances/not-process-complete/{appId}/{instanceGuid:guid}")]
    public async Task<ActionResult<AltinnInstanceSummaryResponse>> GetNotProcessCompleteInstance(
        [FromRoute] string appId,
        [FromRoute] Guid instanceGuid
    )
    {
        var instance = await altinnRecoveryService.GetNonCompletedInstanceByAppId(
            appId,
            instanceGuid
        );
        if (instance == null)
            return NotFound();

        return Ok(instance.ToResponse(Url, appId));
    }

    [HttpGet(
        "instances/not-process-complete/{appId}/{instanceGuid:guid}/documents/{documentId:guid}/download"
    )]
    public async Task<IActionResult> DownloadNotProcessCompleteInstanceDocument(
        [FromRoute] string appId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] Guid documentId
    )
    {
        var document = await altinnRecoveryService.GetDocumentForNonCompletedInstance(
            appId,
            instanceGuid,
            documentId
        );

        if (document == null)
            return NotFound();

        if (document.DocumentContent.CanSeek)
        {
            document.DocumentContent.Position = 0;
        }

        var contentType = document.FileMetadata.ContentType ?? "application/octet-stream";
        var fileName = document.FileMetadata.Filename ?? documentId.ToString();
        return File(document.DocumentContent, contentType, WebUtility.UrlEncode(fileName));
    }

    [HttpGet("subscriptions/{appId}")]
    public async Task<ActionResult<AltinnEventsSubscription>> GetSubscriptionByAltinnAppId(
        [FromRoute] string appId
    )
    {
        var activeAltinnId = await subscriptionService.GetActiveAltinnSubscriptionId(appId);
        if (activeAltinnId == null)
            return NotFound(
                $"We did not find an internal reference to an Altinn subscription for the appId {appId}."
            );
        var altinnSubscription = await altinnRegistrationService.GetAltinnRegistrationById(
            (int)activeAltinnId
        );
        return altinnSubscription != null
            ? Ok(altinnSubscription)
            : NotFound(
                $"We did not got any subscription details from Altinn for the provided AltinnId `{activeAltinnId}`."
            );
    }

    [HttpPost("subscriptions/{id}/retrigger-altinn-validation")]
    public async Task<ActionResult> RetriggerAltinnValidation([FromRoute] int id)
    {
        var found = await subscriptionService.RetriggerAltinnValidation(id);
        return found ? Ok() : NotFound($"No Altinn subscription found with id '{id}'.");
    }

    [HttpPost("start-recovery-job/{appId}")]
    public async Task<ActionResult<List<RecoveryJobResult>>> PostRecoveryRequest(
        [FromRoute] string? appId,
        CancellationToken cancellationToken
    )
    {
        List<RecoveryJobResult> resultList = [];
        if (string.IsNullOrEmpty(appId))
        {
            var allInstancesResult =
                await altinnRecoveryService.GetAllNonCompletedInstancesForRegisteredApps();
            foreach (var (app, instances) in allInstancesResult)
            {
                resultList.Add(
                    await instances.RunRecoveryJob(
                        app,
                        meldingService,
                        logger,
                        apiMeters,
                        cancellationToken
                    )
                );
            }
        }
        else
        {
            IEnumerable<AltinnInstanceSummary> instances =
                await altinnRecoveryService.GetNonCompletedInstancesByAppId(appId)
                ?? throw new ArgumentException($"No registration found for appId: {appId}");

            resultList.Add(
                await instances.RunRecoveryJob(
                    appId,
                    meldingService,
                    logger,
                    apiMeters,
                    cancellationToken
                )
            );
        }
        return resultList;
    }
}

public record NonCompletedInstancesResult
{
    public required string AppId { get; init; }

    public required IEnumerable<AltinnMetadata> NonCompletedInstances { get; init; }
}

public record NotProcessCompleteInstancesResult
{
    public required string AppId { get; init; }
    public required IReadOnlyList<AltinnInstanceSummaryResponse> Instances { get; init; }
}

public record AltinnInstanceSummaryResponse
{
    public required AltinnMetadata Metadata { get; init; }
    public required AltinnDocumentResponse SkjemaAsPdf { get; init; }
    public AltinnDocumentResponse? StructuredData { get; init; }
    public required IReadOnlyList<AltinnDocumentResponse> Attachments { get; init; }
}

public record AltinnDocumentResponse
{
    public required Guid AltinnId { get; init; }
    public string? AltinnDataType { get; init; }
    public FileScanResult? FileScanResult { get; init; }
    public string? ContentType { get; init; }
    public string? Filename { get; init; }
    public string? DownloadUrl { get; init; }
}

file static class AltinnInstanceSummaryResponseExtensions
{
    public static AltinnInstanceSummaryResponse ToResponse(
        this AltinnInstanceSummary instance,
        IUrlHelper url,
        string appId
    )
    {
        return new AltinnInstanceSummaryResponse
        {
            Metadata = instance.Metadata,
            SkjemaAsPdf = instance.SkjemaAsPdf.ToResponse(
                url,
                appId,
                instance.Metadata.InstanceGuid
            ),
            StructuredData = instance.StructuredData?.ToResponse(
                url,
                appId,
                instance.Metadata.InstanceGuid
            ),
            Attachments = instance
                .Attachments.Select(attachment =>
                    attachment.ToResponse(url, appId, instance.Metadata.InstanceGuid)
                )
                .ToList(),
        };
    }

    private static AltinnDocumentResponse ToResponse(
        this AltinnDocument document,
        IUrlHelper url,
        string appId,
        Guid instanceGuid
    )
    {
        return new AltinnDocumentResponse
        {
            AltinnId = document.FileMetadata.AltinnId,
            AltinnDataType = document.FileMetadata.AltinnDataType,
            FileScanResult = document.FileMetadata.FileScanResult,
            ContentType = document.FileMetadata.ContentType,
            Filename = document.FileMetadata.Filename,
            DownloadUrl = url.Action(
                nameof(AltinnController.DownloadNotProcessCompleteInstanceDocument),
                new
                {
                    appId,
                    instanceGuid,
                    documentId = document.FileMetadata.AltinnId,
                }
            ),
        };
    }
}
