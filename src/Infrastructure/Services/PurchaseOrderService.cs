using Application.DTOs.Inventory;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly AppDbContext _db;
    public PurchaseOrderService(AppDbContext db) { _db = db; }

    public async Task<PurchaseOrderResponse> CreateAsync(Guid actorUserId, Guid branchId, PurchaseOrderCreateRequest r, CancellationToken ct = default)
    {
        if (branchId == Guid.Empty) throw new ForbiddenException("Branch context required.");
        if (r.SupplierId == Guid.Empty) throw new ValidationException("Validation failed", new[] { "SupplierId is required." });
        if (r.Items is null || r.Items.Count == 0) throw new ValidationException("Validation failed", new[] { "At least one item required." });

        var supplierExists = await _db.Suppliers.AnyAsync(x => x.Id == r.SupplierId && !x.IsDeleted, ct);
        if (!supplierExists) throw new NotFoundException("Supplier not found");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var orderNo = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var po = new PurchaseOrder { BranchId = branchId, SupplierId = r.SupplierId, OrderNo = orderNo, Status = PurchaseOrderStatus.Draft, Notes = r.Notes?.Trim() };
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync(ct);

        foreach (var it in r.Items)
        {
            if (it.PartId == Guid.Empty || it.Qty <= 0 || it.UnitCost < 0) throw new ValidationException("Validation failed", new[] { "Invalid item values." });
            _db.PurchaseOrderItems.Add(new PurchaseOrderItem { PurchaseOrderId = po.Id, PartId = it.PartId, Qty = it.Qty, UnitCost = it.UnitCost, ReceivedQty = 0m });
        }
        await _db.SaveChangesAsync(ct);

        if (r.JobPartRequestIds != null && r.JobPartRequestIds.Any())
        {
            var requests = await _db.JobPartRequests
                .Where(x => r.JobPartRequestIds.Contains(x.Id) && x.BranchId == branchId && !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var req in requests)
            {
                req.PurchaseOrderId = po.Id;
                req.SupplierId = po.SupplierId;
                req.Status = JobPartRequestStatus.Ordered;
                req.OrderedAt = DateTimeOffset.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);

        return Map(po);
    }

    public async Task<PurchaseOrderResponse> SubmitAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default)
    {
        var po = await Load(branchId, id, ct);
        if (po.Status != PurchaseOrderStatus.Draft) throw new DomainException("Only Draft can be submitted", 409);
        po.Status = PurchaseOrderStatus.Ordered;
        po.OrderedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(po);
    }

    public async Task<PurchaseOrderResponse> ReceiveAsync(Guid actorUserId, Guid branchId, Guid id, PurchaseOrderReceiveRequest r, CancellationToken ct = default)
    {
        var po = await Load(branchId, id, ct);
        if (po.Status is PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Received) throw new DomainException("Cannot receive", 409);
        if (r.Items is null || r.Items.Count == 0) throw new ValidationException("Validation failed", new[] { "Items required." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var items = await _db.PurchaseOrderItems.Where(x => x.PurchaseOrderId == po.Id && !x.IsDeleted).ToListAsync(ct);
        foreach (var recv in r.Items)
        {
            var line = items.FirstOrDefault(x => x.PartId == recv.PartId);
            if (line is null) throw new DomainException("Part not in PO", 409);
            if (recv.ReceiveQty <= 0) throw new ValidationException("Validation failed", new[] { "ReceiveQty must be > 0." });
            if (line.ReceivedQty + recv.ReceiveQty > line.Qty) throw new DomainException("Over-receive not allowed", 409);

            line.ReceivedQty += recv.ReceiveQty;

            // upsert PartStock
            var stock = await _db.PartStocks.FirstOrDefaultAsync(x => x.BranchId == branchId && x.LocationId == r.LocationId && x.PartId == recv.PartId && !x.IsDeleted, ct);
            if (stock is null)
            {
                stock = new PartStock { BranchId = branchId, LocationId = r.LocationId, PartId = recv.PartId, QuantityOnHand = 0m };
                _db.PartStocks.Add(stock);
                await _db.SaveChangesAsync(ct);
            }
            stock.QuantityOnHand += recv.ReceiveQty;

            _db.StockLedgers.Add(new StockLedger
            {
                BranchId = branchId,
                LocationId = r.LocationId,
                PartId = recv.PartId,
                MovementType = StockMovementType.Purchase,
                ReferenceType = "PURCHASE_ORDER",
                ReferenceId = po.Id,
                QuantityDelta = recv.ReceiveQty,
                UnitCost = recv.UnitCost,
                Notes = null,
                PerformedByUserId = actorUserId,
                PerformedAt = DateTimeOffset.UtcNow
            });
        }

        var allReceived = items.All(x => x.ReceivedQty >= x.Qty);
        po.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        if (allReceived) po.ReceivedAt = DateTimeOffset.UtcNow;

        // Auto-mark linked requests as Arrived
        var linkedRequests = await _db.JobPartRequests
            .Where(x => x.PurchaseOrderId == po.Id && x.Status == JobPartRequestStatus.Ordered && !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var req in linkedRequests)
        {
            if (r.Items.Any(i => i.PartId == req.PartId))
            {
                req.Status = JobPartRequestStatus.Arrived;
                req.ArrivedAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Map(po);
    }

    public async Task<PageResponse<PurchaseOrderResponse>> GetPagedAsync(Guid branchId, PageRequest r, CancellationToken ct = default)
    {
        var q = _db.PurchaseOrders.AsNoTracking().Where(x => !x.IsDeleted && x.BranchId == branchId);
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderByDescending(x => x.CreatedAt).Skip((page-1)*size).Take(size)
            .Select(x => new PurchaseOrderResponse(x.Id, x.BranchId, x.SupplierId, x.OrderNo, x.Status, x.OrderedAt, x.ReceivedAt, x.Notes))
            .ToListAsync(ct);
        return new PageResponse<PurchaseOrderResponse>(items, total, page, size);
    }

    public async Task<PurchaseOrderResponse> GetByIdAsync(Guid branchId, Guid id, CancellationToken ct = default)
    {
        var po = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId && !x.IsDeleted, ct);
        if (po is null) throw new NotFoundException("PO not found");
        return Map(po);
    }

    private async Task<PurchaseOrder> Load(Guid branchId, Guid id, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id && x.BranchId == branchId && !x.IsDeleted, ct);
        if (po is null) throw new NotFoundException("PO not found");
        return po;
    }

    private static PurchaseOrderResponse Map(PurchaseOrder x) => new(x.Id, x.BranchId, x.SupplierId, x.OrderNo, x.Status, x.OrderedAt, x.ReceivedAt, x.Notes);
}
