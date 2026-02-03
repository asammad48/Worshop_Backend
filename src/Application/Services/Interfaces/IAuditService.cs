using Application.DTOs.Audit;
using Application.Pagination;

namespace Application.Services.Interfaces;

public interface IAuditService
{
    Task<PageResponse<AuditLogResponse>> GetPagedAsync(Guid? branchId, PageRequest req, CancellationToken ct=default);
}
