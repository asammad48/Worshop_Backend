using Application.DTOs.Inventory;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    public InventoryService(AppDbContext db) { _db = db; }

    public async Task<SupplierResponse> CreateSupplierAsync(SupplierCreateRequest r, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new ValidationException("Validation failed", new[] { "Name is required." });
        var e = new Supplier { Name = r.Name.Trim(), Phone = r.Phone?.Trim(), Email = r.Email?.Trim(), Address = r.Address?.Trim() };
        _db.Suppliers.Add(e);
        await _db.SaveChangesAsync(ct);
        return new SupplierResponse(e.Id, e.Name, e.Phone, e.Email, e.Address);
    }

    public async Task<PageResponse<SupplierResponse>> GetSuppliersAsync(PageRequest r, CancellationToken ct = default)
    {
        var q = _db.Suppliers.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToLower();
            q = q.Where(x => x.Name.ToLower().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.Name).Skip((page-1)*size).Take(size)
            .Select(x => new SupplierResponse(x.Id, x.Name, x.Phone, x.Email, x.Address)).ToListAsync(ct);
        return new PageResponse<SupplierResponse>(items, total, page, size);
    }

    public async Task<PartResponse> CreatePartAsync(PartCreateRequest r, CancellationToken ct = default)
    {
        var sku = (r.Sku ?? "").Trim().ToUpperInvariant();
        var name = (r.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Validation failed", new[] { "Sku and Name are required." });

        var exists = await _db.Parts.AnyAsync(x => x.Sku == sku && !x.IsDeleted, ct);
        if (exists) throw new DomainException("SKU already exists", 409);

        var e = new Part { Sku = sku, Name = name, Brand = r.Brand?.Trim(), Unit = r.Unit?.Trim() };
        _db.Parts.Add(e);
        await _db.SaveChangesAsync(ct);
        return new PartResponse(e.Id, e.Sku, e.Name, e.Brand, e.Unit);
    }

    public async Task<PageResponse<PartResponse>> GetPartsAsync(PageRequest r, CancellationToken ct = default)
    {
        var q = _db.Parts.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToUpperInvariant();
            q = q.Where(x => x.Sku.Contains(s) || x.Name.ToUpper().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.Sku).Skip((page-1)*size).Take(size)
            .Select(x => new PartResponse(x.Id, x.Sku, x.Name, x.Brand, x.Unit)).ToListAsync(ct);
        return new PageResponse<PartResponse>(items, total, page, size);
    }

    public async Task<LocationResponse> CreateLocationAsync(Guid branchId, LocationCreateRequest r, CancellationToken ct = default)
    {
        var code = (r.Code ?? "").Trim().ToUpperInvariant();
        var name = (r.Name ?? "").Trim();
        if (branchId == Guid.Empty) throw new ForbiddenException("Branch context required.");
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Validation failed", new[] { "Code and Name are required." });

        var exists = await _db.Locations.AnyAsync(x => x.BranchId == branchId && x.Code == code && !x.IsDeleted, ct);
        if (exists) throw new DomainException("Location code already exists", 409);

        var e = new Location { BranchId = branchId, Code = code, Name = name, IsActive = true };
        _db.Locations.Add(e);
        await _db.SaveChangesAsync(ct);
        return new LocationResponse(e.Id, e.BranchId, e.Code, e.Name, e.IsActive);
    }

    public async Task<PageResponse<LocationResponse>> GetLocationsAsync(Guid branchId, PageRequest r, CancellationToken ct = default)
    {
        var q = _db.Locations.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToUpperInvariant();
            q = q.Where(x => x.Code.Contains(s) || x.Name.ToUpper().Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.Code).Skip((page-1)*size).Take(size)
            .Select(x => new LocationResponse(x.Id, x.BranchId, x.Code, x.Name, x.IsActive)).ToListAsync(ct);
        return new PageResponse<LocationResponse>(items, total, page, size);
    }

    public async Task<PageResponse<StockItemResponse>> GetStockAsync(Guid branchId, PageRequest r, Guid? locationId = null, Guid? partId = null, CancellationToken ct = default)
    {
        var q = _db.PartStocks.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        if (locationId is not null) q = q.Where(x => x.LocationId == locationId);
        if (partId is not null) q = q.Where(x => x.PartId == partId);
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.PartId).Skip((page-1)*size).Take(size)
            .Select(x => new StockItemResponse(x.PartId, x.LocationId, x.QuantityOnHand)).ToListAsync(ct);
        return new PageResponse<StockItemResponse>(items, total, page, size);
    }

    public async Task AdjustStockAsync(Guid actorUserId, Guid branchId, StockAdjustRequest r, CancellationToken ct = default)
    {
        if (r.QuantityDelta == 0) throw new ValidationException("Validation failed", new[] { "QuantityDelta cannot be 0." });
        if (string.IsNullOrWhiteSpace(r.Reason)) throw new ValidationException("Validation failed", new[] { "Reason is required." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var stock = await _db.PartStocks.FirstOrDefaultAsync(x => x.BranchId == branchId && x.LocationId == r.LocationId && x.PartId == r.PartId && !x.IsDeleted, ct);
        if (stock is null)
        {
            stock = new PartStock { BranchId = branchId, LocationId = r.LocationId, PartId = r.PartId, QuantityOnHand = 0m };
            _db.PartStocks.Add(stock);
            await _db.SaveChangesAsync(ct);
        }

        var newQty = stock.QuantityOnHand + r.QuantityDelta;
        if (newQty < 0) throw new DomainException("Negative stock not allowed", 409);

        stock.QuantityOnHand = newQty;

        var move = r.QuantityDelta > 0 ? StockMovementType.AdjustmentPlus : StockMovementType.AdjustmentMinus;
        _db.StockLedgers.Add(new StockLedger
        {
            BranchId = branchId,
            LocationId = r.LocationId,
            PartId = r.PartId,
            MovementType = move,
            ReferenceType = "ADJUSTMENT",
            ReferenceId = null,
            QuantityDelta = r.QuantityDelta,
            UnitCost = null,
            Notes = r.Reason.Trim(),
            PerformedByUserId = actorUserId,
            PerformedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<PageResponse<LedgerRowResponse>> GetLedgerAsync(Guid branchId, PageRequest r, CancellationToken ct = default)
    {
        var q = _db.StockLedgers.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderByDescending(x => x.PerformedAt).Skip((page-1)*size).Take(size)
            .Select(x => new LedgerRowResponse(x.Id, x.BranchId, x.LocationId, x.PartId, x.MovementType, x.ReferenceType, x.ReferenceId, x.QuantityDelta, x.UnitCost, x.Notes, x.PerformedByUserId, x.PerformedAt))
            .ToListAsync(ct);
        return new PageResponse<LedgerRowResponse>(items, total, page, size);
    }
}
