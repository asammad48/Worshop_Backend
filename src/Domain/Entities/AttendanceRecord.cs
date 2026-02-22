using Domain.Enums;

namespace Domain.Entities;

public sealed class AttendanceRecord : BaseEntity
{
    public Guid? BranchId { get; set; }
    public Guid EmployeeUserId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTimeOffset? CheckInAt { get; set; }
    public DateTimeOffset? CheckOutAt { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public AttendanceSource Source { get; set; }
    public string? Note { get; set; }

    // Navigation properties
    public Branch? Branch { get; set; }
    public User EmployeeUser { get; set; } = null!;
}
