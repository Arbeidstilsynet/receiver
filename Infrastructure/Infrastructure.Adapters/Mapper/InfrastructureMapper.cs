using Arbeidstilsynet.Common.Altinn.Model.Api.Response;
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Model;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Mapster;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Mapper;

internal class InfrastructureMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<AltinnSubscriptionEntity, AltinnConnection>()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .Map(target => target.InternalId, src => src.Id)
            .Map(target => target.AltinnAppId, src => src.AppIdentifier)
            .Map(target => target.SubscriptionId, src => src.SubscriptionId);

        config
            .NewConfig<SubscriptionEntity, ConsumerManifest>()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .Map(target => target.ConsumerName, src => src.ConsumerName)
            .Map(target => target.AppRegistrations, src => src.ToAppRegistrations());

        config
            .NewConfig<AltinnSubscription, AltinnEventsSubscription>()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .Map(target => target.Id, src => src.Id)
            .Map(target => target.CallbackUrl, src => src.EndPoint)
            .Map(target => target.SourceFilter, src => src.SourceFilter)
            .Map(target => target.Consumer, src => src.Consumer)
            .Map(target => target.CreatedBy, src => src.CreatedBy)
            .Map(target => target.Created, src => src.Created);

        config
            .NewConfig<MeldingEntity, Domain.Data.Melding>()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .Map(target => target.MainContentId, src => src.GetMainContentId())
            .Map(target => target.StructuredDataId, src => src.GetStructuredDataId())
            .Map(target => target.AttachmentIds, src => src.GetAttachmentIds());

        config
            .NewConfig<DocumentEntity, Domain.Data.Document>()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .Map(target => target.DocumentId, src => src.Id)
            .Map(target => target.FileMetadata, src => src.Adapt<FileMetadata>());

        config
            .NewConfig<DocumentEntity, FileMetadata>()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible);
    }
}

file static class MappingExtensions
{
    public static List<AppRegistration> ToAppRegistrations(this SubscriptionEntity subscription)
    {
        var altinnApps = subscription.RegisteredAltinnApps.Select(a => new AppRegistration
        {
            AppId = a.AppIdentifier,
            MessageSource = MessageSource.Altinn,
        });

        var apiApps = subscription.RegisteredApiApps.Select(a => new AppRegistration
        {
            AppId = a.AppIdentifier,
            MessageSource = MessageSource.Api,
        });

        return altinnApps.Concat(apiApps).ToList();
    }

    public static Guid? GetMainContentId(this MeldingEntity melding)
    {
        return melding
            .Documents.FirstOrDefault(d => d.DocumentType == DocumentType.MainContent)
            ?.Id;
    }

    public static Guid? GetStructuredDataId(this MeldingEntity melding)
    {
        return melding
            .Documents.FirstOrDefault(d => d.DocumentType == DocumentType.StructuredData)
            ?.Id;
    }

    public static List<Guid> GetAttachmentIds(this MeldingEntity melding)
    {
        return melding
            .Documents.Where(d => d.DocumentType == DocumentType.Attachment)
            .Select(d => d.Id)
            .ToList();
    }
}
