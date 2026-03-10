using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Domain.Ports.Infrastructure.Dto;
using Mapster;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<
                PaginationResponse<Melding>,
                Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App.PaginationResponse<Melding>
            >()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .PreserveReference(true);
    }
}
