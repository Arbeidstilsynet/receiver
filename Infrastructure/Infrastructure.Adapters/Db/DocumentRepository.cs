using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;

internal interface IInternalDocumentRepository
{
    Task<string?> GetInternalDocumentReferenceAsync(Guid documentId, CancellationToken cancellationToken);
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

    private ReceiverDbContext DbContext
    {
        get
        {
            _dbContext.Database.EnsureCreated();
            return _dbContext;
        }
    }

    public async Task<List<Document>> GetAllDocumentsForMelding(Guid meldingId, CancellationToken cancellationToken)
    {
        return await DbContext
            .Documents.Where(d => d.MeldingId == meldingId)
            .Select(s => _mapper.Map<Document>(s))
            .ToListAsync(cancellationToken);
    }

    // internal method, should not be exposed via interface
    public async Task<string?> GetInternalDocumentReferenceAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return (await DbContext.Documents.FindAsync([documentId], cancellationToken: cancellationToken))?.InternalDocumentReference;
    }

    public async Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var entity = await DbContext.Documents.FindAsync([documentId], cancellationToken: cancellationToken);
        if (entity != null)
        {
            return _mapper.Map<Document>(entity);
        }
        return null;
    }
}
