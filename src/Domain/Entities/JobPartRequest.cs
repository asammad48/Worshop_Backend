using Domain.Enums;

namespace Domain.Entities;

public sealed class JobPartRequest : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    public Guid PartId { get; set; }
    public Part? Part { get; set; }

    public decimal Qty { get; set; }

    public string StationCode { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? OrderedAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }

    public Guid? StationSignedByUserId { get; set; }
    public User? StationSignedByUser { get; set; }

    public Guid? OfficeSignedByUserId { get; set; }
    public User? OfficeSignedByUser { get; set; }

    public JobPartRequestStatus Status { get; set; } = JobPartRequestStatus.Requested;

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
