using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Storage;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Shouldly;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Test.Storage;

public class UploadRequestExtensionsTests
{
    [Fact]
    public void CreateGcsObjectName_ShouldReturnCorrectFormat()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var meldingId = Guid.NewGuid();

        var request = new UploadRequest()
        {
            Document = new Document
            {
                DocumentId = documentId,
                MeldingId = meldingId,
                FileMetadata = null!,
                ScanResult = default,
            },
            InputStream = null!,
        };

        // Act
        var result = request.CreateGcsObjectName();

        // Assert
        result.ShouldBe($"{meldingId}/{documentId}");
    }
}
