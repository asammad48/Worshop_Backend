using Domain.Enums;

namespace Domain.Entities;

public sealed class JobLineItem : BaseEntity
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public JobLineItemType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }

    public Guid? PartId { get; set; }
    public Part? Part { get; set; }

    public Guid? JobPartRequestId { get; set; }
    public JobPartRequest? JobPartRequest { get; set; }
}
