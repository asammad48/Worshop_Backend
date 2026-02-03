namespace Application.Pagination;

public sealed record PageResponse<T>(IReadOnlyList<T> Items,int TotalCount,int PageNumber,int PageSize);
