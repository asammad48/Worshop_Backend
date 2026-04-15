namespace Application.DTOs.JobCards;

public sealed class JobCardPageRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
