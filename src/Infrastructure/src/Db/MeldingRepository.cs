using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
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

    public async Task<Melding?> GetMelding(
        Guid meldingId,
        CancellationToken cancellationToken,
        bool includeDeleted = false
    )
    {
        using var activity = Tracer.Source.StartActivity();
        var entity = await _dbContext
            .Meldinger.Include(m => m.Documents)
            .FirstOrDefaultAsync(f => f.Id == meldingId, cancellationToken);
        if (entity != null)
        {
            return MapMelding(entity, includeDeleted);
        }
        return null;
    }

    public Task<AttachmentDetachmentResponse> DetachAttachment(
        Guid meldingId,
        AttachmentDetachmentRequest request,
        CancellationToken cancellationToken
    ) => ChangeAttachmentDeletion(meldingId, request, restore: false, cancellationToken);

    public Task<AttachmentDetachmentResponse> RestoreAttachment(
        Guid meldingId,
        AttachmentDetachmentRequest request,
        CancellationToken cancellationToken
    ) => ChangeAttachmentDeletion(meldingId, request, restore: true, cancellationToken);

    private async Task<AttachmentDetachmentResponse> ChangeAttachmentDeletion(
        Guid meldingId,
        AttachmentDetachmentRequest request,
        bool restore,
        CancellationToken cancellationToken
    )
    {
        using var activity = Tracer.Source.StartActivity(
            restore ? "RestoreAttachment" : "DetachAttachment"
        );
        activity?.SetTag("melding.id", meldingId);
        activity?.SetTag("document.id", request.DocumentId);

        if (meldingId == Guid.Empty || request.DocumentId == Guid.Empty)
        {
            throw new AttachmentDetachmentNotAllowedException(
                "Melding and document IDs must be non-empty."
            );
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            request.Apply ? IsolationLevel.ReadCommitted : IsolationLevel.RepeatableRead,
            cancellationToken
        );

        var melding = request.Apply
            ? await _dbContext
                .Meldinger.FromSqlInterpolated(
                    $"SELECT * FROM public.meldinger WHERE \"Id\" = {meldingId} FOR UPDATE"
                )
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken)
            : await _dbContext
                .Meldinger.AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == meldingId, cancellationToken);

        if (melding == null)
        {
            throw new MeldingNotFoundException(meldingId);
        }

        var documents = request.Apply
            ? await _dbContext
                .Documents.FromSqlInterpolated(
                    $"SELECT * FROM public.documents WHERE \"MeldingId\" = {meldingId} ORDER BY \"Id\" FOR UPDATE"
                )
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : await _dbContext
                .Documents.AsNoTracking()
                .Where(d => d.MeldingId == meldingId)
                .OrderBy(d => d.Id)
                .ToListAsync(cancellationToken);

        var target = documents.FirstOrDefault(document => document.Id == request.DocumentId);
        if (target == null)
        {
            throw new AttachmentDetachmentNotAllowedException(
                "The document does not belong to this melding."
            );
        }

        var alreadyDetached = target.DocumentType == DocumentType.DetachedAttachment;
        if (!restore && !alreadyDetached && target.DocumentType != DocumentType.Attachment)
        {
            throw new AttachmentDetachmentNotAllowedException(
                "Only attachment documents can be soft-deleted. Main content and structured data cannot be deleted."
            );
        }

        if (request.Apply && string.IsNullOrWhiteSpace(request.ExpectedRevision))
        {
            throw new AttachmentDetachmentNotAllowedException(
                "An expected revision is required when applying a deletion or restoration."
            );
        }

        var revisionBeforeApply = CreateRevision(melding, documents);
        if (
            request.Apply
            && !string.Equals(
                request.ExpectedRevision,
                revisionBeforeApply,
                StringComparison.Ordinal
            )
        )
        {
            throw new AttachmentDetachmentConflictException();
        }

        var attachmentIdsBefore = GetAttachmentIds(documents);
        var attachmentIdsAfter = restore
            ? alreadyDetached
                ? attachmentIdsBefore.Append(target.Id).Order().ToList()
                : attachmentIdsBefore
            : attachmentIdsBefore.Where(id => id != target.Id).ToList();

        var revision = revisionBeforeApply;
        if (request.Apply && (restore ? alreadyDetached : !alreadyDetached))
        {
            var trackedTarget = _dbContext
                .ChangeTracker.Entries<DocumentEntity>()
                .FirstOrDefault(entry => entry.Entity.Id == target.Id);
            if (trackedTarget != null)
            {
                trackedTarget.State = EntityState.Detached;
            }

            _dbContext.Documents.Attach(target);
            target.DocumentType = restore
                ? DocumentType.Attachment
                : DocumentType.DetachedAttachment;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dbContext.Entry(target).ReloadAsync(cancellationToken);
            revision = CreateRevision(melding, documents);
        }

        await transaction.CommitAsync(cancellationToken);

        return new AttachmentDetachmentResponse
        {
            MeldingId = melding.Id,
            DocumentId = target.Id,
            ApplicationId = melding.ApplicationId,
            MainContentId = documents
                .FirstOrDefault(d => d.DocumentType == DocumentType.MainContent)
                ?.Id,
            StructuredDataId = documents
                .FirstOrDefault(d => d.DocumentType == DocumentType.StructuredData)
                ?.Id,
            AttachmentIdsBefore = attachmentIdsBefore,
            AttachmentIdsAfter = attachmentIdsAfter,
            FileName = target.FileName ?? string.Empty,
            ContentType = target.ContentType ?? string.Empty,
            Revision = revision,
            Applied = request.Apply,
            AlreadyDetached = alreadyDetached,
        };
    }

    public async Task<IReadOnlyList<Melding>> GetMeldingerByShortId(
        string shortId,
        CancellationToken cancellationToken,
        bool includeDeleted = false
    )
    {
        using var activity = Tracer.Source.StartActivity();

        // ShortId is a stored, indexed computed column holding the trailing 12 hex characters of the
        // GUID. Postgres renders a uuid as a lowercase string, so we compare against the lowercased
        // short id to keep the lookup case-insensitive while remaining index-sargable.
        var normalizedShortId = shortId.ToLowerInvariant();

        var entities = await _dbContext
            .Meldinger.Include(m => m.Documents)
            .Where(m => m.ShortId == normalizedShortId)
            .ToListAsync(cancellationToken);

        return entities.Select(e => MapMelding(e, includeDeleted)).ToList();
    }

    public async Task<PaginationResponse<Melding>> GetMeldinger(
        int pageSize,
        int pageNumber = 1,
        bool includeDeleted = false
    )
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
            Items = [.. itemsWithDocument.Select(s => MapMelding(s, includeDeleted))],
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalRecords = totalRecords,
        };
    }

    private Melding MapMelding(MeldingEntity entity, bool includeDeleted)
    {
        var melding = _mapper.Map<Melding>(entity);
        return includeDeleted
            ? melding with
            {
                AttachmentIds = entity
                    .Documents.Where(d =>
                        d.DocumentType is DocumentType.Attachment or DocumentType.DetachedAttachment
                    )
                    .Select(d => d.Id)
                    .ToList(),
            }
            : melding;
    }

    private static List<Guid> GetAttachmentIds(IEnumerable<DocumentEntity> documents) =>
        documents
            .Where(d => d.DocumentType == DocumentType.Attachment)
            .Select(d => d.Id)
            .Order()
            .ToList();

    private static string CreateRevision(
        MeldingEntity melding,
        IEnumerable<DocumentEntity> documents
    )
    {
        using var content = new MemoryStream();
        using (var writer = new Utf8JsonWriter(content))
        {
            writer.WriteStartObject();
            writer.WriteString("meldingId", melding.Id);
            writer.WriteString("source", melding.Source.ToString());
            writer.WriteString("applicationId", melding.ApplicationId);
            writer.WriteString("updatedAt", FormatTimestamp(melding.UpdatedAt));
            WriteTags(writer, "tags", melding.Tags);
            WriteTags(writer, "internalTags", melding.InternalTags);

            writer.WriteStartArray("documents");
            foreach (var document in documents.OrderBy(d => d.Id))
            {
                writer.WriteStartObject();
                writer.WriteString("id", document.Id);
                writer.WriteString("type", document.DocumentType.ToString());
                writer.WriteString("updatedAt", FormatTimestamp(document.UpdatedAt));
                writer.WriteString("fileName", document.FileName);
                writer.WriteString("contentType", document.ContentType);
                WriteTags(writer, "tags", document.Tags);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        var hash = SHA256.HashData(content.GetBuffer().AsSpan(0, (int)content.Length));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static void WriteTags(
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyDictionary<string, string> tags
    )
    {
        writer.WriteStartObject(propertyName);
        foreach (var tag in tags.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            writer.WriteString(tag.Key, tag.Value);
        }
        writer.WriteEndObject();
    }

    private static string FormatTimestamp(DateTime value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
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
