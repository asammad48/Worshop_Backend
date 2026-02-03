using Application.DTOs.Inventory;
using Application.Pagination;

namespace Application.Services.Interfaces;
public interface IInventoryService
{
    Task<SupplierResponse> CreateSupplierAsync(SupplierCreateRequest r, CancellationToken ct=default);
    Task<PageResponse<SupplierResponse>> GetSuppliersAsync(PageRequest r, CancellationToken ct=default);

    Task<PartResponse> CreatePartAsync(PartCreateRequest r, CancellationToken ct=default);
    Task<PageResponse<PartResponse>> GetPartsAsync(PageRequest r, CancellationToken ct=default);

    Task<LocationResponse> CreateLocationAsync(Guid branchId, LocationCreateRequest r, CancellationToken ct=default);
    Task<PageResponse<LocationResponse>> GetLocationsAsync(Guid branchId, PageRequest r, CancellationToken ct=default);

    Task<PageResponse<StockItemResponse>> GetStockAsync(Guid branchId, PageRequest r, Guid? locationId=null, Guid? partId=null, CancellationToken ct=default);
    Task AdjustStockAsync(Guid actorUserId, Guid branchId, StockAdjustRequest r, CancellationToken ct=default);

    Task<PageResponse<LedgerRowResponse>> GetLedgerAsync(Guid branchId, PageRequest r, CancellationToken ct=default);
}
