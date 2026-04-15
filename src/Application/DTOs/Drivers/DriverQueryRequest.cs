using Application.Pagination;

namespace Application.DTOs.Drivers;

public sealed class DriverQueryRequest : PageRequest
{
    public Guid? CustomerId { get; set; }
}
