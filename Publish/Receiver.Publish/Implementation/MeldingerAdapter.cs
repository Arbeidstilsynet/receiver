using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Ports;

namespace Arbeidstilsynet.Receiver.Implementation;

internal class MeldingerAdapter(IMeldingerClient meldingerClient) : IMeldingerAdapter
{
    public async Task<T> GetMainAltinnDocument<T>(Melding melding)
    {
        if (melding.Source != MessageSource.Altinn)
        {
            throw new InvalidOperationException(
                $"This method is intended to be used by messages which come from Altinn. The message's source was '{melding.Source}' (Id: {melding.Id})"
            );
        }
        var mainDocumentMetadata =
            await meldingerClient.GetDocument(melding.Id, melding.ContentId)
            ?? throw new InvalidOperationException(
                $"Could not get the message's document metadata. Id: {melding.Id}, ContentId: {melding.ContentId}"
            );
        if (!mainDocumentMetadata.IsDocumentSafeToUse)
        {
            throw new InvalidOperationException(
                $"The message's document is not safe to use based on anti virus scan result. We do not proceed here, process this message manually. Id: {melding.Id}, ContentId: {melding.ContentId}"
            );
        }

        if (mainDocumentMetadata.FileMetadata.ContentType != "application/json")
        {
            throw new InvalidOperationException(
                $"We only support application/json altinn documents. The provided content type {mainDocumentMetadata.FileMetadata.ContentType} is not supported."
            );
        }

        var document =
            await meldingerClient.DownloadDocument(melding.Id, melding.ContentId)
            ?? throw new InvalidOperationException(
                $"Could not download the message's main document. Id: {melding.Id}, ContentId: {melding.ContentId}"
            );

        return JsonSerializer.Deserialize<T>(document)
            ?? throw new InvalidOperationException(
                $"Could not xml deserialize the message's main document to type {typeof(T)}. Correlated Message Id: {melding.Id}"
            );
    }
}
