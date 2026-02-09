using Arbeidstilsynet.Common.Altinn.Model.Adapter;
using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.API.Adapters.Extensions;
using Argon;
using Shouldly;
using AltinnFileMetadata = Arbeidstilsynet.Common.Altinn.Model.Adapter.FileMetadata;

namespace Arbeidstilsynet.MeldingerReceiver.API.Adapters.Test;

public class AltinnMapperTests
{
    private const string TestAppId = "test-skjema";
    private const string TestOrg = "dat";
    private const string TestOwnerPartyid = "123123";
    private const string TestInstanceId = "11827765-ed99-44a4-a88b-54a32f7627b6";

    private readonly VerifySettings _verifySettings = new();

    public AltinnMapperTests()
    {
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.DontScrubGuids();
        _verifySettings.AddExtraSettings(jsonSettings =>
        {
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
        });
    }

    [Fact]
    async Task MapAltinnSummaryToPostMeldingRequest_VerifyResult()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(DateTime.Now);
        //assert
        await Verify(result, _verifySettings);
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenCalledWithCompleteAltinnSummary_MapsMainDocument()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(DateTime.Now);
        //assert
        result.MainContent.InputStream.ShouldBeSameAs(summary.SkjemaAsPdf.DocumentContent);
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenCalledWithCompleteAltinnSummary_MapsStructuredData()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(DateTime.Now);
        //assert
        result.StructuredData.ShouldNotBeNull();
        result.StructuredData.InputStream.ShouldBeSameAs(summary.StructuredData!.DocumentContent);
    }

    [Fact]
    public void MapAltinnSummaryToPostMeldingRequest_WhenCalledWithCompleteAltinnSummary_MapsAttachment()
    {
        //arrange
        var summary = GetCompleteAltinnSummary();
        //act
        var result = summary.MapAltinnSummaryToPostMeldingRequest(DateTime.Now);
        //assert
        result.Attachments.ShouldHaveSingleItem();
        result.Attachments[0].InputStream.ShouldBeSameAs(summary.Attachments[0].DocumentContent);
    }

    private static AltinnInstanceSummary GetCompleteAltinnSummary()
    {
        return new AltinnInstanceSummary
        {
            Metadata = new AltinnMetadata
            {
                App = TestAppId,
                InstanceGuid = Guid.Parse(TestInstanceId),
                Org = TestOrg,
                InstanceOwnerPartyId = TestOwnerPartyid,
            },

            StructuredData = new AltinnDocument
            {
                DocumentContent = new MemoryStream("{ \"key\": \"value\" }"u8.ToArray()),
                FileMetadata = new AltinnFileMetadata
                {
                    AltinnId = Guid.NewGuid(),
                    ContentType = "application/json",
                    AltinnDataType = "structured-data",
                    Filename = "structured-data.json",
                    FileScanResult = FileScanResult.Clean,
                },
            },
            SkjemaAsPdf = new AltinnDocument()
            {
                DocumentContent = new MemoryStream("maincContent"u8.ToArray()),
                FileMetadata = new AltinnFileMetadata
                {
                    AltinnId = Guid.NewGuid(),
                    ContentType = "application/pdf",
                    AltinnDataType = "ref-data-as-pdf",
                    Filename = "main-data.pdf",
                    FileScanResult = FileScanResult.Clean,
                },
            },
            Attachments =
            [
                new AltinnDocument
                {
                    DocumentContent = new MemoryStream("attachmentContent"u8.ToArray()),
                    FileMetadata = new AltinnFileMetadata
                    {
                        AltinnId = Guid.NewGuid(),
                        ContentType = "application/pdf",
                        AltinnDataType = "some-type",
                        Filename = "etellerannet.pdf",
                        FileScanResult = FileScanResult.Clean,
                    },
                },
            ],
        };
    }
}
