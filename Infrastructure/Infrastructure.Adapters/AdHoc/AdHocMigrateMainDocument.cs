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
        Guid mainDocument,
        Guid structuredData,
        IEnumerable<Guid> attachments,
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

        var attachementIds = attachments.ToList();

        var eachId = attachementIds.Append(mainDocument).Append(structuredData).ToList();

        if (eachId.Any(id => id == Guid.Empty))
        {
            throw new InvalidOperationException(
                "One or more provided document references are empty. Please ensure all document references are valid."
            );
        }

        if (eachId.Distinct().Count() != eachId.Count)
        {
            throw new InvalidOperationException(
                "The provided document references contain duplicates. Please ensure all document references are unique."
            );
        }

        var hangingDocRefs = eachId.Except(melding.Documents.Select(d => d.Id)).ToList();

        if (hangingDocRefs.Count != 0)
        {
            throw new InvalidOperationException(
                $"The following document references do not exist on the melding and cannot be added: {string.Join(", ", hangingDocRefs)}"
            );
        }

        foreach (var doc in melding.Documents)
        {
            if (doc.Id == mainDocument)
            {
                doc.DocumentType = DocumentType.MainContent;
            }
            else if (doc.Id == structuredData)
            {
                doc.DocumentType = DocumentType.StructuredData;
            }
            else if (attachementIds.Contains(doc.Id))
            {
                doc.DocumentType = DocumentType.Attachment;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<Melding>(melding);
    }
}
