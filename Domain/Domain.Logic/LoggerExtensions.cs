using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal static class LoggerExtensions
{
    public static void LogPostActionError(
        this ILogger logger,
        string actionName,
        Melding melding,
        Exception exception
    )
    {
        logger.LogError(
            exception,
            "Action failed: {actionName}, MeldingId: {meldingId}",
            actionName,
            melding.Id
        );
    }

    public static void LogSkippedUnsafeDocuments(
        this ILogger logger,
        Guid meldingId,
        List<Document> unsafeDocuments
    )
    {
        logger.LogWarning(
            "Processing melding with ID: {meldingId}. Skipping unsafe documents: {@unsafeDocuments}",
            meldingId,
            unsafeDocuments
        );
    }
}
