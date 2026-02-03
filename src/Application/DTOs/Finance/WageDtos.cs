namespace Application.DTOs.Finance;
public sealed record WagePayRequest(Guid EmployeeUserId, decimal Amount, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, string? Notes);
public sealed record WagePayResponse(Guid Id, Guid BranchId, Guid EmployeeUserId, decimal Amount, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, DateTimeOffset PaidAt, Guid PaidByUserId, string? Notes);
