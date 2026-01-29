using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Jobs;
using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi.Controllers;

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
                $"We did not find an internal reference to an altinn subscription for the appId {appId}."
            );
        var altinnSubscription = await altinnRegistrationService.GetAltinnRegistrationById(
            (int)activeAltinnId
        );
        return altinnSubscription != null
            ? Ok(altinnSubscription)
            : NotFound(
                $"We did not got any subscrition details from altinn for the provided altinnId `{activeAltinnId}`."
            );
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
