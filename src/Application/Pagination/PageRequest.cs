namespace Application.Pagination;

public sealed record PageRequest(int PageNumber=1,int PageSize=20,string? Search=null,string? SortBy=null,string SortDirection="asc");
