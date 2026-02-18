using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.AdHoc;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.AdHoc;

internal class AdHocMigrateMainDocument : IAdHocMigrateMainDocument
{
    private readonly ReceiverDbContext _dbContext;
    private readonly IMapper _mapper;

    public AdHocMigrateMainDocument(ReceiverDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Melding?> MigrateMainDocument(
        Guid meldingId,
        Guid newMainContent,
        CancellationToken cancellationToken
    )
    {
        var melding = await _dbContext
            .Meldinger.Include(x => x.Documents)
            .FirstOrDefaultAsync(m => m.Id == meldingId, cancellationToken);
        if (melding == null)
        {
            return null;
        }

        if (newMainContent == Guid.Empty || melding.Documents.All(d => d.Id != newMainContent))
        {
            throw new ArgumentException(
                "The provided new main content ID is invalid or does not exist in the melding.",
                nameof(newMainContent)
            );
        }

        foreach (var doc in melding.Documents)
        {
            if (doc.Id == newMainContent)
            {
                doc.DocumentType = DocumentType.MainContent;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<Melding>(melding);
    }
}
