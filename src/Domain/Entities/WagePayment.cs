namespace Domain.Entities;
public sealed class WagePayment: BaseEntity{ public Guid BranchId{get;set;} public Guid EmployeeUserId{get;set;} public decimal Amount{get;set;} public DateTimeOffset PeriodStart{get;set;} public DateTimeOffset PeriodEnd{get;set;} public DateTimeOffset PaidAt{get;set;} public Guid PaidByUserId{get;set;} public string? Notes{get;set;} }
