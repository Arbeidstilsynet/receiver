using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;

internal class MeldingRepository : IMeldingRepository
{
    private readonly ReceiverDbContext _dbContext;
    private readonly IMapper _mapper;

    public MeldingRepository(ReceiverDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Melding> CreateMelding(
        CreateMeldingRequest createMeldingRequest,
        CancellationToken cancellationToken
    )
    {
        using var activity = Tracer.Source.StartActivity();

        var meldingEntity = createMeldingRequest.ToMeldingEntity();

        var updatedEntity = await _dbContext.Meldinger.AddAsync(meldingEntity, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await updatedEntity.ReloadAsync(cancellationToken);
        return _mapper.Map<Melding>(updatedEntity.Entity);
    }

    public async Task<Melding?> GetMelding(Guid meldingId, CancellationToken cancellationToken)
    {
        using var activity = Tracer.Source.StartActivity();
        var entity = await _dbContext
            .Meldinger.Include(m => m.Documents)
            .FirstOrDefaultAsync(f => f.Id == meldingId, cancellationToken);
        if (entity != null)
        {
            return _mapper.Map<Melding>(entity);
        }
        return null;
    }

    public async Task<IReadOnlyList<Melding>> GetMeldingerByShortId(
        string shortId,
        CancellationToken cancellationToken
    )
    {
        using var activity = Tracer.Source.StartActivity();

        // The short id is the trailing segment of the GUID. Postgres renders a uuid as a
        // lowercase, hyphenated string (e.g. "22222222-2222-2222-2222-222222222222"), so we
        // match meldinger whose id text ends with the (lowercased) short id.
        var suffix = shortId.ToLowerInvariant();

        var entities = await _dbContext
            .Meldinger.Include(m => m.Documents)
            .Where(m => EF.Functions.Like(m.Id.ToString()!, "%" + suffix))
            .ToListAsync(cancellationToken);

        return entities.Select(e => _mapper.Map<Melding>(e)).ToList();
    }

    public async Task<PaginationResponse<Melding>> GetMeldinger(int pageSize, int pageNumber = 1)
    {
        using var activity = Tracer.Source.StartActivity();
        var baseQuery = _dbContext.Meldinger.Select(s => new { s.Id, s.ReceivedAt });
        int totalRecords = await baseQuery.CountAsync();
        int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var items = await baseQuery
            .OrderByDescending(b => b.ReceivedAt)
            .ThenBy(b => b.Id)
            .Skip(pageNumber == 1 ? 0 : (pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var meldingIds = items.Select(s => s.Id).ToList();
        var itemsWithDocument = await _dbContext
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
