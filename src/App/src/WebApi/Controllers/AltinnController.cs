using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Arbeidstilsynet.MeldingerReceiver.App.Jobs;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Arbeidstilsynet.Receiver.Model.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.App.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AltinnController(
    IAltinnRecoveryService altinnRecoveryService,
    IAltinnStorageService altinnStorageService,
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

    [HttpGet("non-completed-instances/{appId}")]
    public async Task<ActionResult<IEnumerable<AltinnMetadata>>> GetAllNonCompletedInstances(
        [FromRoute] string appId
    )
    {
        return Ok(await altinnRecoveryService.GetMetadataForNonCompletedInstancesByAppId(appId));
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

    [HttpPost("process/{appId}/{instanceGuid:guid}")]
    [ProducesResponseType<PostMeldingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostMeldingResponse>> ProcessInstance(
        [FromRoute] string appId,
        [FromRoute] Guid instanceGuid,
        CancellationToken cancellationToken
    )
    {
        var processableInstance = (
            await altinnRecoveryService.GetNonCompletedInstancesByAppId(appId)
        )?.FirstOrDefault(instance => instance.Metadata.InstanceGuid == instanceGuid);

        if (processableInstance == null)
        {
            var instanceMetadata = await altinnStorageService.GetInstance(
                instanceGuid,
                cancellationToken
            );

            if (instanceMetadata == null)
            {
                return NotFound();
            }

            return BadRequest(
                $"Instance '{instanceGuid}' for appId '{appId}' is not ready to be processed."
            );
        }

        var melding = await processableInstance.ProcessSingleInstance(
            appId,
            meldingService,
            apiMeters,
            cancellationToken
        );

        return Ok(new PostMeldingResponse { MeldingId = melding.Id });
    }

    [HttpGet("instances/{instanceGuid:guid}/data-elements")]
    public async Task<ActionResult<IReadOnlyList<DataElement>>> GetInstanceDataElements(
        [FromRoute] Guid instanceGuid,
        CancellationToken ct
    )
    {
        var result = await altinnStorageService.GetDataElements(instanceGuid, ct);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("instances/{instanceGuid:guid}/data-elements/{dataElementId:guid}")]
    public async Task<ActionResult> DownloadInstanceDataElement(
        [FromRoute] Guid instanceGuid,
        [FromRoute] Guid dataElementId,
        CancellationToken ct
    )
    {
        var metadata = await altinnStorageService.GetDataElement(instanceGuid, dataElementId, ct);
        if (metadata is not { ContentType: { Length: > 0 } contentType })
            return NotFound();
        var content = await altinnStorageService.GetDataElementContent(
            instanceGuid,
            dataElementId,
            ct
        );
        if (content == null)
            return NotFound();
        return File(content, contentType, metadata.Filename ?? dataElementId.ToString());
    }

    [HttpGet("instances/{instanceGuid:guid}/metadata")]
    public async Task<ActionResult<AltinnInstance>> GetInstanceMetadata(
        [FromRoute] Guid instanceGuid,
        CancellationToken ct
    )
    {
        var result = await altinnStorageService.GetInstance(instanceGuid, ct);
        return result != null ? Ok(result) : NotFound();
    }
}

public record NonCompletedInstancesResult
{
    public required string AppId { get; init; }

    public required IEnumerable<AltinnMetadata> NonCompletedInstances { get; init; }
}
