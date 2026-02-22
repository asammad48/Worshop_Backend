using Application.DTOs.Dashboard;
using Application.Pagination;
using Domain.Enums;

namespace Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewResponse> GetOverviewAsync(DashboardOverviewQuery query, UserRole role, Guid currentUserId);
    Task<PageResponse<JobCardAlertRow>> GetJobCardAlertsAsync(Guid? branchId, string? status, int? minDaysInShop, bool? hasRoadblocker, ApprovalRole? requiresApprovalRole, int pageNumber, int pageSize);
    Task<InventoryDashboardResponse> GetInventoryDashboardAsync(Guid? branchId, bool belowReorderOnly, bool? pendingPoOnly, bool? pendingTransfersOnly, int pageNumber, int pageSize);
    Task<PageResponse<EmployeeKpiRow>> GetEmployeeKpisAsync(Guid? branchId, DateOnly? from, DateOnly? to, Guid? employeeUserId, int pageNumber, int pageSize);
}
