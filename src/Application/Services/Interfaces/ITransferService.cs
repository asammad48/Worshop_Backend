using Application.DTOs.Inventory;
using Application.Pagination;

namespace Application.Services.Interfaces;
public interface ITransferService
{
    Task<TransferResponse> CreateAsync(Guid actorUserId, Guid fromBranchId, TransferCreateRequest r, CancellationToken ct=default);
    Task<TransferResponse> RequestAsync(Guid actorUserId, Guid fromBranchId, Guid id, CancellationToken ct=default);
    Task<TransferResponse> ShipAsync(Guid actorUserId, Guid fromBranchId, Guid id, CancellationToken ct=default);
    Task<TransferResponse> ReceiveAsync(Guid actorUserId, Guid toBranchId, Guid id, CancellationToken ct=default);
    Task<PageResponse<TransferResponse>> GetPagedAsync(Guid branchId, PageRequest r, CancellationToken ct=default);
    Task<TransferResponse> GetByIdAsync(Guid id, CancellationToken ct=default);
}
