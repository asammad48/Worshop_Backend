using Domain.Enums;
namespace Application.DTOs.Finance;
public sealed record ExpenseCreateRequest(ExpenseCategory Category, decimal Amount, string? Description, DateTimeOffset ExpenseAt);
public sealed record ExpenseResponse(Guid Id, Guid BranchId, ExpenseCategory Category, decimal Amount, string? Description, DateTimeOffset ExpenseAt, string? BranchName = null);
