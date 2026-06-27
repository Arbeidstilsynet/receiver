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

    [HttpGet("instances/{instanceGuid}/data-elements")]
    public async Task<ActionResult<IReadOnlyList<DataElement>>> GetInstanceDataElements(
        [FromRoute] Guid instanceGuid,
        CancellationToken ct
    )
    {
        var result = await altinnRecoveryService.GetDataElementsForInstance(instanceGuid, ct);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("instances/{instanceGuid}/data-elements/{dataElementId}")]
    public async Task<ActionResult> DownloadInstanceDataElement(
        [FromRoute] Guid instanceGuid,
        [FromRoute] Guid dataElementId,
        CancellationToken ct
    )
    {
        var result = await altinnRecoveryService.DownloadDataElement(
            instanceGuid,
            dataElementId,
            ct
        );
        if (result == null)
            return NotFound();
        return File(result.Content, result.ContentType, result.Filename);
    }

    [HttpGet("instances/{instanceGuid}/metadata")]
    public async Task<ActionResult<AltinnInstance>> GetInstanceMetadata(
        [FromRoute] Guid instanceGuid,
        CancellationToken ct
    )
    {
        var result = await altinnRecoveryService.GetInstanceMetadata(instanceGuid, ct);
        return result != null ? Ok(result) : NotFound();
    }
}

public record NonCompletedInstancesResult
{
    public required string AppId { get; init; }

    public required IEnumerable<AltinnMetadata> NonCompletedInstances { get; init; }
}
