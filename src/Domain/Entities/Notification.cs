using Domain.Enums;

namespace Domain.Entities;

public sealed class Notification : BaseEntity
{
    public string Type { get; set; } = string.Empty; // JOB_CARD, INVENTORY, APPROVAL, ATTENDANCE
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RefType { get; set; }
    public Guid? RefId { get; set; }
    public bool IsRead { get; set; }

    // Target user (optional, if null might be branch-wide or global depending on logic)
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Scoping
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
