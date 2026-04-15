namespace Application.DTOs.Drivers;

public sealed class DriverQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";
    public Guid? CustomerId { get; set; }
}
