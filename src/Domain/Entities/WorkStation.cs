namespace Domain.Entities;
public sealed class WorkStation : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string Code { get; set; } = string.Empty;   // e.g., LIFT_1
    public string Name { get; set; } = string.Empty;   // e.g., Lift Bay 1
    public bool IsActive { get; set; } = true;
}
