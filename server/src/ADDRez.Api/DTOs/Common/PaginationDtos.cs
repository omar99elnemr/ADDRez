namespace ADDRez.Api.DTOs.Common;

public record PaginatedResponse<T>(
    IEnumerable<T> Data,
    int CurrentPage,
    int LastPage,
    int Total,
    int PerPage
);

public record PaginationQuery
{
    public int Page { get; init; } = 1;
    public int PerPage { get; init; } = 15;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortDir { get; init; } = "asc";
}
