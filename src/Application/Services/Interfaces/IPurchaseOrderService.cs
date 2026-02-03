using Application.DTOs.Inventory;
using Application.Pagination;

namespace Application.Services.Interfaces;
public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse> CreateAsync(Guid actorUserId, Guid branchId, PurchaseOrderCreateRequest r, CancellationToken ct=default);
    Task<PurchaseOrderResponse> SubmitAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct=default);
    Task<PurchaseOrderResponse> ReceiveAsync(Guid actorUserId, Guid branchId, Guid id, PurchaseOrderReceiveRequest r, CancellationToken ct=default);
    Task<PageResponse<PurchaseOrderResponse>> GetPagedAsync(Guid branchId, PageRequest r, CancellationToken ct=default);
    Task<PurchaseOrderResponse> GetByIdAsync(Guid branchId, Guid id, CancellationToken ct=default);
}
