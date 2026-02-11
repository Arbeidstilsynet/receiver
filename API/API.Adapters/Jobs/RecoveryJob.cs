using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.WebApi;
using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Quartz;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Jobs;

internal class RecoveryJob(
    IAltinnRecoveryService altinnRecoveryService,
    IMeldingService meldingService,
    ILogger<RecoveryJob> logger,
    ApiMeters apiMeters
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var nonCompletedInstances =
            await altinnRecoveryService.GetAllNonCompletedInstancesForRegisteredApps();

        foreach (var (appId, instances) in nonCompletedInstances)
        {
            await instances.RunRecoveryJob(
                appId,
                meldingService,
                logger,
                apiMeters,
                context.CancellationToken
            );
        }
    }
}

public record RecoveryJobResult
{
    public required string AppId { get; init; }
    public int OriginalCount { get; init; }

    public int ResolvedCount { get; init; }
}

internal static class RecoveryJobExtensions
{
    public static async Task<RecoveryJobResult> RunRecoveryJob(
        this IEnumerable<AltinnInstanceSummary> instances,
        string appId,
        IMeldingService meldingService,
        ILogger logger,
        ApiMeters apiMeters,
        CancellationToken cancellationToken
    )
    {
        var instancesList = instances.ToList();
        var originalCount = instancesList.Count;
        var jobsLeftForAppId = instancesList.Count;
        var processedCount = 0;

        apiMeters.UpdateRecoveryCounts(jobsLeftForAppId, appId);

        foreach (var instance in instancesList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Recovery job was cancelled. There are {JobsLeft} jobs left for {AppId} to be processed.",
                    jobsLeftForAppId,
                    appId
                );
                return new()
                {
                    AppId = appId,
                    OriginalCount = originalCount,
                    ResolvedCount = processedCount,
                };
            }

            apiMeters.MeldingReceived(MessageSource.Altinn, appId);
            var request = instance.MapAltinnSummaryToPostMeldingRequest();
            var melding = await meldingService.ProcessMelding(request, cancellationToken);
            apiMeters.MeldingProcessed(melding);
            apiMeters.RegisterMeldingDuration(melding);
            jobsLeftForAppId--;
            processedCount++;
        }

        apiMeters.UpdateRecoveryCounts(jobsLeftForAppId, appId);

        return new()
        {
            AppId = appId,
            OriginalCount = originalCount,
            ResolvedCount = processedCount,
        };
    }
}
