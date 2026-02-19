using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Ports;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.Receiver.Implementation;

internal class MeldingerAdapter(IMeldingerClient meldingerClient, ILogger<MeldingerAdapter> logger)
    : IMeldingerAdapter
{
    public async Task<TStructuredData?> FetchStructuredData<TStructuredData>(Melding melding)
    {
        if (melding.StructuredDataId is not { } structuredDataId)
            return default(TStructuredData);

        var mainDocumentMetadata = await meldingerClient.GetDocument(melding.Id, structuredDataId);

        if (mainDocumentMetadata == null)
        {
            logger.LogError(
                "Could not retrieve the message's structured data metadata. Id: {MeldingId}, DocumentId: {DocumentId}",
                melding.Id,
                melding.StructuredDataId
            );
            return default(TStructuredData);
        }

        if (!mainDocumentMetadata.IsDocumentSafeToUse)
        {
            logger.LogError(
                "The message's structured data is not safe to use based on anti virus scan result. Id: {MeldingId}, DocumentId: {DocumentId}",
                melding.Id,
                melding.StructuredDataId
            );
            return default(TStructuredData);
        }

        if (mainDocumentMetadata.FileMetadata.ContentType != "application/json")
        {
            logger.LogError(
                "The message's structured data has an unsupported content type. Id: {MeldingId}, DocumentId: {DocumentId}, ContentType: {ContentType}",
                melding.Id,
                melding.StructuredDataId,
                mainDocumentMetadata.FileMetadata.ContentType
            );
            return default(TStructuredData);
        }

        var document = await meldingerClient.DownloadDocument(melding.Id, structuredDataId);

        if (document is not { Length: > 0 })
        {
            logger.LogError(
                "The message's structured data could not be downloaded or is empty. Id: {MeldingId}, DocumentId: {DocumentId}",
                melding.Id,
                melding.StructuredDataId
            );
            return default(TStructuredData);
        }

        try
        {
            var structuredData = JsonSerializer.Deserialize<TStructuredData>(document);

            if (structuredData is null)
            {
                logger.LogError(
                    "The message's structured data could not be deserialized. Id: {MeldingId}, DocumentId: {DocumentId}",
                    melding.Id,
                    melding.StructuredDataId
                );
                return default(TStructuredData);
            }

            return structuredData;
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "The message's structured data could not be deserialized due to a JSON exception. Id: {MeldingId}, DocumentId: {DocumentId}",
                melding.Id,
                melding.StructuredDataId
            );
            return default(TStructuredData);
        }
    }
}
