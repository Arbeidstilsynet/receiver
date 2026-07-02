namespace Arbeidstilsynet.MeldingerReceiver.Domain.Ports.App;

public record PaginationResponse<T>
{
    public List<T> Items { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages { get; init; }
}
