using Application.DTOs.Finance;
using Application.Pagination;

namespace Application.Services.Interfaces;
public interface IFinanceService
{
    Task<ExpenseResponse> CreateExpenseAsync(Guid actorUserId, Guid branchId, ExpenseCreateRequest r, CancellationToken ct=default);
    Task<PageResponse<ExpenseResponse>> GetExpensesAsync(Guid branchId, PageRequest r, DateTimeOffset? from=null, DateTimeOffset? to=null, CancellationToken ct=default);

    Task<WagePayResponse> PayWageAsync(Guid actorUserId, Guid branchId, WagePayRequest r, CancellationToken ct=default);
    Task<PageResponse<WagePayResponse>> GetWagesAsync(Guid branchId, PageRequest r, DateTimeOffset? from=null, DateTimeOffset? to=null, CancellationToken ct=default);
}
