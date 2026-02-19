using System.Text;
using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Implementation;
using Arbeidstilsynet.Receiver.Ports;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Arbeidstilsynet.Receiver.Test;

public class MeldingerAdapterTests
{
    private readonly IMeldingerClient _meldingerClient;
    private readonly ILogger<MeldingerAdapter> _logger;
    private readonly MeldingerAdapter _sut;

    public MeldingerAdapterTests()
    {
        _meldingerClient = Substitute.For<IMeldingerClient>();
        _logger = Substitute.For<ILogger<MeldingerAdapter>>();
        _sut = new MeldingerAdapter(_meldingerClient, _logger);
    }

    [Fact]
    public async Task FetchStructuredData_WhenStructuredDataIdIsNull_ReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding() with
        {
            StructuredDataId = null,
        };

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        await _meldingerClient.DidNotReceive().GetDocument(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task FetchStructuredData_WhenGetDocumentReturnsNull_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        _meldingerClient
            .GetDocument(melding.Id, melding.StructuredDataId!.Value)
            .Returns((Document?)null);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o =>
                    o.ToString()!
                        .Contains("Could not retrieve the message's structured data metadata")
                ),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task FetchStructuredData_WhenDocumentIsNotSafeToUse_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument() with { ScanResult = DocumentScanResult.Infected };
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o =>
                    o.ToString()!.Contains("not safe to use based on anti virus scan result")
                ),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        await _meldingerClient.DidNotReceive().DownloadDocument(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task FetchStructuredData_WhenContentTypeIsNotJson_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument() with
        {
            FileMetadata = new FileMetadata
            {
                FileName = "test.xml",
                ContentType = "application/xml",
            },
        };
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("unsupported content type")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        await _meldingerClient.DidNotReceive().DownloadDocument(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task FetchStructuredData_WhenDownloadReturnsNull_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument();
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);
        _meldingerClient
            .DownloadDocument(melding.Id, melding.StructuredDataId!.Value)
            .Returns((Stream?)null);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("could not be downloaded or is empty")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task FetchStructuredData_WhenDownloadReturnsEmptyStream_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument();
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);
        _meldingerClient
            .DownloadDocument(melding.Id, melding.StructuredDataId!.Value)
            .Returns(Substitute.For<MemoryStream>());

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("could not be downloaded or is empty")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task FetchStructuredData_WhenDeserializationFails_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument();
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);

        var invalidJson = "{ invalid json }";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
        _meldingerClient
            .DownloadDocument(melding.Id, melding.StructuredDataId!.Value)
            .Returns(stream);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("could not be deserialized")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task FetchStructuredData_WhenDeserializationReturnsNull_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument();
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);

        var nullJson = "null";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(nullJson));
        _meldingerClient
            .DownloadDocument(melding.Id, melding.StructuredDataId!.Value)
            .Returns(stream);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("could not be deserialized")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task FetchStructuredData_WhenEverythingIsValid_ReturnsDeserializedData()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument();
        var expectedData = new TestStructuredData
        {
            Id = Guid.NewGuid(),
            Name = "Test Data",
            Value = 42,
        };

        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);

        var jsonData = JsonSerializer.Serialize(expectedData);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
        _meldingerClient
            .DownloadDocument(melding.Id, melding.StructuredDataId!.Value)
            .Returns(stream);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedData.Id);
        result.Name.ShouldBe(expectedData.Name);
        result.Value.ShouldBe(expectedData.Value);

        _logger
            .DidNotReceive()
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task FetchStructuredData_WhenDocumentScanResultIsUnknown_LogsErrorAndReturnsDefault()
    {
        // Arrange
        var melding = CreateTestMelding();
        var document = CreateTestDocument() with { ScanResult = DocumentScanResult.Unknown };
        _meldingerClient.GetDocument(melding.Id, melding.StructuredDataId!.Value).Returns(document);

        // Act
        var result = await _sut.FetchStructuredData<TestStructuredData>(melding);

        // Assert
        result.ShouldBeNull();
        _logger
            .Received(1)
            .Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Is<object>(o =>
                    o.ToString()!.Contains("not safe to use based on anti virus scan result")
                ),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    private static Melding CreateTestMelding()
    {
        return new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Altinn,
            ApplicationId = "test-app",
            ReceivedAt = DateTime.UtcNow,
            MainContentId = Guid.NewGuid(),
            StructuredDataId = Guid.NewGuid(),
        };
    }

    private static Document CreateTestDocument()
    {
        return new Document
        {
            DocumentId = Guid.NewGuid(),
            MeldingId = Guid.NewGuid(),
            FileMetadata = new FileMetadata
            {
                FileName = "test.json",
                ContentType = "application/json",
            },
            ScanResult = DocumentScanResult.Clean,
        };
    }

    private record TestStructuredData
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Value { get; init; }
    }
}
