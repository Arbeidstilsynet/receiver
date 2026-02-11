using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;

internal class MeldingRepository : IMeldingRepository
{
    private readonly InfrastructureAdaptersDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<MeldingRepository> _logger;

    public MeldingRepository(
        InfrastructureAdaptersDbContext dbContext,
        IMapper mapper,
        ILogger<MeldingRepository> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    private InfrastructureAdaptersDbContext DbContext
    {
        get
        {
            _dbContext.Database.EnsureCreated();
            return _dbContext;
        }
    }

    public async Task<Melding> CreateMelding(CreateMeldingRequest createMeldingRequest, CancellationToken cancellationToken)
    {
        using var activity = Tracer.Source.StartActivity();

        var meldingEntity = createMeldingRequest.ToMeldingEntity();

        var updatedEntity = await DbContext.Meldinger.AddAsync(meldingEntity, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
        await updatedEntity.ReloadAsync(cancellationToken);
        return _mapper.Map<Melding>(updatedEntity.Entity);
    }

    public async Task<Melding?> GetMeldingAsync(Guid meldingId, CancellationToken cancellationToken)
    {
        using var activity = Tracer.Source.StartActivity("Get Melding");
        var entity = await DbContext
            .Meldinger.Include(m => m.Documents)
            .FirstOrDefaultAsync(f => f.Id == meldingId, cancellationToken);
        if (entity != null)
        {
            return _mapper.Map<Melding>(entity);
        }
        return null;
    }

    public async Task<PaginationResponse<Melding>> GetMeldingerAsync(
        int pageSize,
        int pageNumber = 1
    )
    {
        using var activity = Tracer.Source.StartActivity("Get Meldinger");
        var baseQuery = DbContext.Meldinger.Select(s => new { s.Id, s.ReceivedAt });
        int totalRecords = await baseQuery.CountAsync();
        int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var items = await baseQuery
            .OrderByDescending(b => b.ReceivedAt)
            .ThenBy(b => b.Id)
            .Skip(pageNumber == 1 ? 0 : (pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var meldingIds = items.Select(s => s.Id).ToList();
        var itemsWithDocument = await DbContext
            .Meldinger.Include(m => m.Documents)
            .Where(w => meldingIds.Contains(w.Id))
            .OrderByDescending(b => b.ReceivedAt)
            .ThenBy(b => b.Id)
            .ToListAsync();
        return new PaginationResponse<Melding>
        {
            Items = [.. itemsWithDocument.Select(s => _mapper.Map<Melding>(s))],
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalRecords = totalRecords,
        };
    }
}

file static class MappingExtensions
{
    public static MeldingEntity ToMeldingEntity(this CreateMeldingRequest createMeldingRequest)
    {
        List<DocumentEntity> documents = [];

        if (createMeldingRequest.MapMainDocument() is { } mainDocument)
        {
            documents.Add(mainDocument);
        }

        if (createMeldingRequest.MapStructuredDocument() is { } structuredDocument)
        {
            documents.Add(structuredDocument);
        }

        documents.AddRange(createMeldingRequest.MapAttachmentDocuments());

        return new MeldingEntity
        {
            Id = createMeldingRequest.Id,
            Source = createMeldingRequest.Source,
            ApplicationId = createMeldingRequest.ApplicationId,
            ReceivedAt = createMeldingRequest.ReceivedAt.ToUniversalTime(),
            Tags = createMeldingRequest.Tags,
            InternalTags = createMeldingRequest.InternalTags,
            Documents = documents,
        };
    }

    public static DocumentEntity? MapMainDocument(this CreateMeldingRequest createMeldingRequest)
    {
        return createMeldingRequest.MainDocumentData?.ToDocumentEntity(
            createMeldingRequest.Id,
            DocumentType.MainContent
        );
    }

    public static DocumentEntity? MapStructuredDocument(
        this CreateMeldingRequest createMeldingRequest
    )
    {
        return createMeldingRequest.StructuredData?.ToDocumentEntity(
            createMeldingRequest.Id,
            DocumentType.StructuredData
        );
    }

    public static IEnumerable<DocumentEntity> MapAttachmentDocuments(
        this CreateMeldingRequest createMeldingRequest
    )
    {
        return createMeldingRequest.AttachmentData.Select(attachment =>
            attachment.ToDocumentEntity(createMeldingRequest.Id, DocumentType.Attachment)
        );
    }

    private static DocumentEntity ToDocumentEntity(
        this DocumentStorageDto storageDto,
        Guid meldingId,
        DocumentType documentType
    )
    {
        return new DocumentEntity
        {
            Id = storageDto.DocumentId,
            MeldingId = meldingId,
            InternalDocumentReference = storageDto.InternalDocumentReference,
            DocumentType = documentType,
            ContentType = storageDto.ContentType,
            FileName = storageDto.FileName,
            ScanResult = storageDto.ScanResult,
            Tags = storageDto.Tags,
        };
    }
}
