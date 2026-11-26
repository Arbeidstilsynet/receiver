using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data.Exceptions;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Model;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db;

internal interface IInternalDocumentRepository
{
    Task<string?> GetInternalDocumentReferenceAsync(
        Guid documentId,
        CancellationToken cancellationToken
    );
}

internal class DocumentRepository : IDocumentRepository, IInternalDocumentRepository
{
    private readonly ReceiverDbContext _dbContext;
    private readonly IMapper _mapper;

    public DocumentRepository(ReceiverDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<List<Document>> GetAllDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext
            .Documents.Where(d => d.MeldingId == meldingId)
            .Select(s => _mapper.Map<Document>(s))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Document>> GetDeletedDocumentsForMelding(
        Guid meldingId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext
            .Documents.IgnoreQueryFilters()
            .Where(d => d.MeldingId == meldingId && d.DeletedAt != null)
            .Select(s => _mapper.Map<Document>(s))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SetDeleted(
        Guid meldingId,
        Guid documentId,
        bool deleted,
        CancellationToken cancellationToken
    )
    {
        var entity = await _dbContext
            .Documents.IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                d => d.Id == documentId && d.MeldingId == meldingId,
                cancellationToken
            );
        if (entity == null)
        {
            return false;
        }

        if (entity.DocumentType != DocumentType.Attachment)
        {
            throw new DocumentDeletionNotAllowedException(documentId);
        }

        if (deleted != (entity.DeletedAt != null))
        {
            entity.DeletedAt = deleted ? DateTime.UtcNow : null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    // internal method, should not be exposed via interface
    public async Task<string?> GetInternalDocumentReferenceAsync(
        Guid documentId,
        CancellationToken cancellationToken
    )
    {
        return (
            await _dbContext.Documents.FindAsync([documentId], cancellationToken: cancellationToken)
        )?.InternalDocumentReference;
    }

    public async Task<Document?> GetDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken
    )
    {
        var entity = await _dbContext.Documents.SingleOrDefaultAsync(
            d => d.Id == documentId,
            cancellationToken
        );
        return entity == null ? null : _mapper.Map<Document>(entity);
    }
}
