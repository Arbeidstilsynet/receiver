namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Ports.Dto;

public record PaginationResponse<T>
{
    public required List<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalRecords { get; init; }
    public required int TotalPages { get; init; }
}
