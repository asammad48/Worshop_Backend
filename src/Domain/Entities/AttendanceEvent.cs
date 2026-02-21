using Domain.Enums;

namespace Domain.Entities;

public sealed class AttendanceEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public AttendanceEventType EventType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Note { get; set; }
}
