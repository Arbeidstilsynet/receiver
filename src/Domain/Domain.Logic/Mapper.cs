using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Mapster;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic;

internal class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<
                PaginationResponse<Melding>,
                Arbeidstilsynet.MeldingerReceiver.API.Ports.PaginationResponse<Melding>
            >()
            .NameMatchingStrategy(NameMatchingStrategy.Flexible)
            .PreserveReference(true);
    }
}
