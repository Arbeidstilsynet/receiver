using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;

internal interface IInternalDocumentRepository
{
    Task<string?> GetInternalDocumentReferenceAsync(Guid documentId);
}

internal class DocumentRepository : IDocumentRepository, IInternalDocumentRepository
{
    private readonly InfrastructureAdaptersDbContext _dbContext;
    private readonly IMapper _mapper;

    public DocumentRepository(InfrastructureAdaptersDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    private InfrastructureAdaptersDbContext DbContext
    {
        get
        {
            _dbContext.Database.EnsureCreated();
            return _dbContext;
        }
    }

    public async Task<List<Document>> GetAllDocumentsForMelding(Guid meldingId)
    {
        return await DbContext
            .Documents.Where(d => d.MeldingId == meldingId)
            .Select(s => _mapper.Map<Document>(s))
            .ToListAsync();
    }

    // internal method, should not be exposed via interface
    public async Task<string?> GetInternalDocumentReferenceAsync(Guid documentId)
    {
        return (await DbContext.Documents.FindAsync(documentId))?.InternalDocumentReference;
    }

    public async Task<Document?> GetDocumentAsync(Guid documentId)
    {
        var entity = await DbContext.Documents.FindAsync(documentId);
        if (entity != null)
        {
            return _mapper.Map<Document>(entity);
        }
        return null;
    }
}
