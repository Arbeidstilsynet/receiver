using Arbeidstilsynet.MeldingerReceiver.API.Ports;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Logic.DependencyInjection;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal class MeldingService : IMeldingService
{
    private readonly IDocumentStorage _documentStorage;
    private readonly IMeldingRepository _meldingRepository;
    private readonly IVirusScanService _virusScanService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IEnumerable<IPostMeldingPersistedAction> _postMeldingActions;
    private readonly IMapper _mapper;
    private readonly ILogger<MeldingService> _logger;

    public MeldingService(
        IDocumentStorage documentStorage,
        IMeldingRepository meldingRepository,
        IVirusScanService virusScanService,
        ISubscriptionService subscriptionService,
        IEnumerable<IPostMeldingPersistedAction> postMeldingActions,
        IMapper mapper,
        ILogger<MeldingService> logger
    )
    {
        _documentStorage = documentStorage;
        _meldingRepository = meldingRepository;
        _virusScanService = virusScanService;
        _subscriptionService = subscriptionService;
        _postMeldingActions = postMeldingActions;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Melding> ProcessMelding(
        PostMeldingRequest request,
        CancellationToken cancellationToken
    )
    {
        var meldingId = request.MeldingId;
        _logger.LogInformation("Received request to process melding for {MeldingId}", meldingId);
        if (
            await _subscriptionService.ShouldMeldingForAppIdBeIgnored(
                request.Source,
                request.ApplicationReference
            )
        )
        {
            throw new InvalidOperationException(
                $"""
                Tried to receive a melding where we do not have a registered consumer yet. 
                This exception occured because the setting `AllowOnlyRegisteredApps` is true 
                and we do not have a matching registration for appId: {request.ApplicationReference} with messageSource: {request.Source} yet.
                """
            );
        }
        var existingMelding = await _meldingRepository.GetMeldingAsync(meldingId);
        if (existingMelding != null)
        {
            await RunPostActions(existingMelding);
            return existingMelding;
        }

        // upload documents
        var (mainDocumentUpload, structuredDocumentUpload, attachmentUploads) =
            await UploadDocuments(meldingId, request, cancellationToken);

        // create and persist melding
        var createMeldingRequest = new CreateMeldingRequest
        {
            Id = meldingId,
            Source = request.Source,
            ApplicationId = request.ApplicationReference,
            ReceivedAt = request.MeldingReceivedAt,
            MainDocumentData = mainDocumentUpload.PersistedDocument,
            StructuredData = structuredDocumentUpload?.PersistedDocument,
            AttachmentData = attachmentUploads.Select(a => a.PersistedDocument).ToList(),
            Tags = request.Metadata,
        };
        var melding = await _meldingRepository.SaveMelding(createMeldingRequest);

        await RunPostActions(melding);
        return melding;
    }

    public async Task<Melding?> GetMelding(GetMeldingRequest request)
    {
        return await _meldingRepository.GetMeldingAsync(request.MeldingId);
    }

    public async Task<API.Ports.PaginationResponse<Melding>> GetMeldinger(
        int? pageNumber = 1,
        int? pageSize = 10
    )
    {
        return _mapper.Map<API.Ports.PaginationResponse<Melding>>(
            await _meldingRepository.GetMeldingerAsync(pageSize ?? 10, pageNumber ?? 1)
        );
    }

    private async Task<(
        UploadResponse mainDocumentUpload,
        UploadResponse? structuredDocumentUpload,
        List<UploadResponse> attachmentUploads
    )> UploadDocuments(
        Guid meldingId,
        PostMeldingRequest request,
        CancellationToken cancellationToken
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var mainContentUploadRequest = request.MainContent.ToUploadRequest(meldingId);
        var mainDocumentUpload = await ProcessUploadRequest(
            mainContentUploadRequest,
            cancellationToken
        );

        var structuredDataUploadRequest = request.StructuredData?.ToUploadRequest(meldingId);
        var structuredDocumentUpload =
            structuredDataUploadRequest == null
                ? null
                : await ProcessUploadRequest(structuredDataUploadRequest, cancellationToken);

        List<UploadResponse> attachmentUploads = [];
        // Process attachments one at a time to avoid loading all streams into memory at once
        foreach (var attachment in request.Attachments)
        {
            var uploadRequest = attachment.ToUploadRequest(meldingId);
            var uploadResponse = await ProcessUploadRequest(uploadRequest, cancellationToken);
            attachmentUploads.Add(uploadResponse);
        }
        return (mainDocumentUpload, structuredDocumentUpload, attachmentUploads);
    }

    private async Task RunPostActions(Melding melding)
    {
        using var activity = Tracer.Source.StartActivity("Executing Post Actions");
        foreach (var action in _postMeldingActions)
        {
            try
            {
                await action.RunPostActionFor(melding);
            }
            catch (Exception exception)
            {
                _logger.LogPostActionError(action.Name, melding, exception);
            }
        }
    }

    private async Task<UploadResponse> ProcessUploadRequest(
        UploadRequest uploadRequest,
        CancellationToken cancellationToken
    )
    {
        var uploadResult = await _documentStorage.Upload(
            uploadRequest,
            cancellationToken: cancellationToken
        );
        if (uploadRequest.Document.ScanResult != DocumentScanResult.Clean)
        {
            await _virusScanService.ScanForVirus(uploadResult, cancellationToken);
        }

        return uploadResult;
    }
}
