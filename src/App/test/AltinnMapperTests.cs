using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.Common.Altinn.Storage.Models;
using Arbeidstilsynet.Common.TestExtensions.Snapshots;
using Arbeidstilsynet.MeldingerReceiver.App.Extensions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using AltinnFileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.App.Test;

public class AltinnMapperTests
{
    private const string TestAppId = "test-skjema";
    private const string TestOrg = "dat";
    private const string TestOwnerPartyid = "123123";
    private const string TestInstanceId = "11827765-ed99-44a4-a88b-54a32f7627b6";
    private const string MainContentId = "abcd1234-5678-90ab-cdef-1234567890ab";
    private const string StructuredDataId = "da158178-b7e1-44a1-bd20-7de5ef2fbc7a";
    private const string AttachmentId = "a013a210-e65d-46cb-8eb8-d41d41756be6";

    private readonly SnapshotSettings _snapshotSettings = new SnapshotSettings()
        .DontScrubGuids()
        .UseDirectory("Snapshots")
        .IncludeDefaultValues()
        .ScrubMembers("InputStream");
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    async Task MapAltinnSummaryToPostMeldingRequest_VerifyResult()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        //assert
        await Snapshot.Verify(result, _snapshotSettings);
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenCalledWithCompleteAltinnSummary_MapsMainDocument()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        //assert
        result.MainContent.ShouldNotBeNull();
        result.MainContent.InputStream.ShouldBeSameAs(summary.SkjemaAsPdf.DocumentContent);
        result.MainContent.DocumentId.ShouldBe(new Guid(MainContentId));
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenCalledWithCompleteAltinnSummary_MapsStructuredData()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        //assert
        result.StructuredData.ShouldNotBeNull();
        result.StructuredData.InputStream.ShouldBeSameAs(summary.StructuredData!.DocumentContent);
        result.StructuredData.DocumentId.ShouldBe(new Guid(StructuredDataId));
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenCalledWithCompleteAltinnSummary_MapsAttachment()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        //assert
        result.Attachments.ShouldHaveSingleItem();
        result.Attachments[0].InputStream.ShouldBeSameAs(summary.Attachments[0].DocumentContent);
        result.Attachments[0].DocumentId.ShouldBe(new Guid(AttachmentId));
    }

    [Fact]
    public async Task MapAltinnSummaryToPostMeldingRequest_AllDocumentsAreInfected_TreatStructuredDataAndMainContentAsClean()
    {
        //arrange
        var summary = GetCompleteAltinnSummary(FileScanResult.Infected);
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        //assert
        await Snapshot.Verify(result, _snapshotSettings);
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenAppIsNull_ThrowsArgumentException()
    {
        var summary = GetCompleteAltinnSummary();

        summary = summary with { Metadata = summary.Metadata with { App = null } };

        Should
            .Throw<ArgumentException>(() => summary.MapAltinnSummaryToPostMeldingRequest(_logger))
            .Message.ShouldContain("app name");
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenContentTypeIsNull_ThrowsArgumentException()
    {
        var summary = GetCompleteAltinnSummary();
        summary = summary with
        {
            SkjemaAsPdf = summary.SkjemaAsPdf with
            {
                FileMetadata = summary.SkjemaAsPdf.FileMetadata with { ContentType = null },
            },
        };

        Should
            .Throw<ArgumentException>(() => summary.MapAltinnSummaryToPostMeldingRequest(_logger))
            .Message.ShouldContain("ContentType");
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenFilenameIsNull_ThrowsArgumentException()
    {
        var summary = GetCompleteAltinnSummary();
        summary = summary with
        {
            SkjemaAsPdf = summary.SkjemaAsPdf with
            {
                FileMetadata = summary.SkjemaAsPdf.FileMetadata with { Filename = null },
            },
        };
        Should
            .Throw<ArgumentException>(() => summary.MapAltinnSummaryToPostMeldingRequest(_logger))
            .Message.ShouldContain("Filename");
    }

    [Theory]
    [InlineData("main")]
    [InlineData("structured")]
    [InlineData("attachment")]
    public void MapAltinnSummaryToPostMeldingRequest_WithFileMetadata_PropagatesDocumentTags(
        string documentType
    )
    {
        var summary = GetCompleteAltinnSummary();
        var document = GetDocument(summary, documentType);
        document.FileMetadata.Metadata.Add("UnntattOffentlighet", "true");
        document.FileMetadata.Metadata.Add("CustomTag", "");

        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        var tags = GetDocumentTags(result, documentType);

        tags.Count.ShouldBe(4);
        tags["AltinnId"].ShouldBe(document.FileMetadata.AltinnId.ToString());
        tags["AltinnDataType"].ShouldBe(document.FileMetadata.AltinnDataType);
        tags["UnntattOffentlighet"].ShouldBe("true");
        tags["CustomTag"].ShouldBe("");
        _logger.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WithEmptyMetadata_KeepsCanonicalTags()
    {
        var summary = GetCompleteAltinnSummary();

        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);

        foreach (var documentType in new[] { "main", "structured", "attachment" })
        {
            var metadata = GetDocument(summary, documentType).FileMetadata;
            var tags = GetDocumentTags(result, documentType);
            tags.Count.ShouldBe(2);
            tags["AltinnId"].ShouldBe(metadata.AltinnId.ToString());
            tags["AltinnDataType"].ShouldBe(metadata.AltinnDataType);
        }
        _logger.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("main", "AltinnId", false)]
    [InlineData("main", "AltinnDataType", false)]
    [InlineData("structured", "AltinnId", false)]
    [InlineData("structured", "AltinnDataType", false)]
    [InlineData("attachment", "AltinnId", false)]
    [InlineData("attachment", "AltinnDataType", false)]
    [InlineData("main", "AltinnId", true)]
    [InlineData("main", "AltinnDataType", true)]
    [InlineData("structured", "AltinnId", true)]
    [InlineData("structured", "AltinnDataType", true)]
    [InlineData("attachment", "AltinnId", true)]
    [InlineData("attachment", "AltinnDataType", true)]
    public void MapAltinnSummaryToPostMeldingRequest_WithReservedMetadataKey_KeepsCanonicalValue(
        string documentType,
        string key,
        bool identicalValue
    )
    {
        var summary = GetCompleteAltinnSummary();
        var metadata = GetDocument(summary, documentType).FileMetadata;
        var canonicalValue =
            key == "AltinnId" ? metadata.AltinnId.ToString() : metadata.AltinnDataType!;
        var value = identicalValue ? canonicalValue : "sensitive-conflicting-value";
        metadata.Metadata.Add(key, value);
        metadata.Metadata.Add("OtherTag", "preserved");

        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);
        var tags = GetDocumentTags(result, documentType);

        tags.Count.ShouldBe(3);
        tags[key].ShouldBe(canonicalValue);
        tags["OtherTag"].ShouldBe("preserved");
        metadata.Metadata[key].ShouldBe(value);
        if (identicalValue)
        {
            _logger.ReceivedCalls().ShouldBeEmpty();
        }
        else
        {
            var call = _logger.ReceivedCalls().ShouldHaveSingleItem();
            call.GetMethodInfo().Name.ShouldBe(nameof(ILogger.Log));
            var arguments = call.GetArguments();
            arguments[0].ShouldBe(LogLevel.Warning);
            var state = arguments[2]
                .ShouldBeAssignableTo<IEnumerable<KeyValuePair<string, object?>>>();
            state.ShouldContain(pair => pair.Key == "MetadataKey" && Equals(pair.Value, key));
            state.ShouldContain(pair =>
                pair.Key == "DocumentId" && Equals(pair.Value, metadata.AltinnId)
            );
            state.ShouldNotContain(pair => Equals(pair.Value, value));
            arguments[2]!.ToString()!.ShouldNotContain(value);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MapAltinnSummaryToPostMeldingRequest_WithoutCanonicalTags_UsesFileMetadataValues(
        string? dataType
    )
    {
        var summary = GetCompleteAltinnSummary();
        summary = summary with
        {
            SkjemaAsPdf = summary.SkjemaAsPdf with
            {
                FileMetadata = summary.SkjemaAsPdf.FileMetadata with
                {
                    AltinnId = Guid.Empty,
                    AltinnDataType = dataType,
                    Metadata = new Dictionary<string, string>
                    {
                        ["AltinnId"] = "metadata-id",
                        ["AltinnDataType"] = "metadata-type",
                    },
                },
            },
        };

        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);

        result.MainContent.DocumentId.ShouldBeNull();
        result.MainContent.Tags.Count.ShouldBe(2);
        result.MainContent.Tags["AltinnId"].ShouldBe("metadata-id");
        result.MainContent.Tags["AltinnDataType"].ShouldBe("metadata-type");
        _logger.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MapAltinnSummaryToPostMeldingRequest_WithoutCanonicalTagsOrMetadata_HasEmptyTags(
        string? dataType
    )
    {
        var summary = GetCompleteAltinnSummary();
        summary = summary with
        {
            SkjemaAsPdf = summary.SkjemaAsPdf with
            {
                FileMetadata = summary.SkjemaAsPdf.FileMetadata with
                {
                    AltinnId = Guid.Empty,
                    AltinnDataType = dataType,
                },
            },
        };

        var result = summary.MapAltinnSummaryToPostMeldingRequest(_logger);

        result.MainContent.DocumentId.ShouldBeNull();
        result.MainContent.Tags.ShouldBeEmpty();
        _logger.ReceivedCalls().ShouldBeEmpty();
    }

    private static AltinnDocument GetDocument(AltinnInstanceSummary summary, string documentType) =>
        documentType switch
        {
            "main" => summary.SkjemaAsPdf,
            "structured" => summary.StructuredData!,
            "attachment" => summary.Attachments[0],
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

    private static Dictionary<string, string> GetDocumentTags(
        CreateMeldingRequest request,
        string documentType
    ) =>
        documentType switch
        {
            "main" => request.MainContent.Tags,
            "structured" => request.StructuredData!.Tags,
            "attachment" => request.Attachments[0].Tags,
            _ => throw new ArgumentOutOfRangeException(nameof(documentType)),
        };

    private static AltinnInstanceSummary GetCompleteAltinnSummary(
        FileScanResult fileScanResult = FileScanResult.Clean
    )
    {
        return new AltinnInstanceSummary
        {
            Metadata = new AltinnMetadata
            {
                App = TestAppId,
                InstanceGuid = Guid.Parse(TestInstanceId),
                Org = TestOrg,
                InstanceOwnerPartyId = TestOwnerPartyid,
                DataValues = [],
            },

            StructuredData = new AltinnDocument
            {
                DocumentContent = new MemoryStream("{ \"key\": \"value\" }"u8.ToArray()),
                FileMetadata = new AltinnFileMetadata
                {
                    AltinnId = new Guid(StructuredDataId),
                    ContentType = "application/json",
                    AltinnDataType = "structured-data",
                    Filename = "structured-data.json",
                    FileScanResult = fileScanResult,
                },
            },
            SkjemaAsPdf = new AltinnDocument()
            {
                DocumentContent = new MemoryStream("maincContent"u8.ToArray()),
                FileMetadata = new AltinnFileMetadata
                {
                    AltinnId = new Guid(MainContentId),
                    ContentType = "application/pdf",
                    AltinnDataType = "ref-data-as-pdf",
                    Filename = "main-data.pdf",
                    FileScanResult = fileScanResult,
                },
            },
            Attachments =
            [
                new AltinnDocument
                {
                    DocumentContent = new MemoryStream("attachmentContent"u8.ToArray()),
                    FileMetadata = new AltinnFileMetadata
                    {
                        AltinnId = new Guid(AttachmentId),
                        ContentType = "application/pdf",
                        AltinnDataType = "some-type",
                        Filename = "etellerannet.pdf",
                        FileScanResult = fileScanResult,
                    },
                },
            ],
        };
    }
}
