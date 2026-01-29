using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;
using Domain.Logic.Test.fixtures;
using MapsterMapper;
using Shouldly;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Arbeidstilsynet.MeldingerReceiver.Domain.Logic.Test;

public class MapperTests : TestBed<DomainLogicTestFixture>
{
    private readonly IMapper _mapper;

    public MapperTests(ITestOutputHelper testOutputHelper, DomainLogicTestFixture fixture)
        : base(testOutputHelper, fixture)
    {
        _mapper = fixture.GetService<IMapper>(testOutputHelper)!;
    }

    [Fact]
    public void MapInfraPagination_To_ApiPagination()
    {
        //arrange
        var melding = new Melding
        {
            Id = Guid.NewGuid(),
            Source = MessageSource.Altinn,
            ApplicationId = "test",
            ReceivedAt = default,
        };
        var infraPagination = new PaginationResponse<Melding>
        {
            Items = [melding],
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 5,
            TotalRecords = 50,
        };
        //act
        var result = _mapper.Map<API.Ports.PaginationResponse<Melding>>(infraPagination);
        //assert
        result.ShouldBeEquivalentTo(
            new API.Ports.PaginationResponse<Melding>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalPages = 5,
                TotalRecords = 50,
                Items = [melding],
            }
        );
    }
}
