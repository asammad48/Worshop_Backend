using Application.DTOs.Inventory;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class TransferService : ITransferService
{
    private readonly AppDbContext _db;
    public TransferService(AppDbContext db) { _db = db; }

    public async Task<TransferResponse> CreateAsync(Guid actorUserId, Guid fromBranchId, TransferCreateRequest r, CancellationToken ct = default)
    {
        if (r.Items is null || r.Items.Count == 0) throw new ValidationException("Validation failed", new[] { "Items required." });
        var no = $"TR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var t = new StockTransfer
        {
            FromBranchId = fromBranchId,
            FromLocationId = r.FromLocationId,
            ToBranchId = r.ToBranchId,
            ToLocationId = r.ToLocationId,
            TransferNo = no,
            Status = StockTransferStatus.Draft,
            Notes = r.Notes?.Trim()
        };
        _db.StockTransfers.Add(t);
        await _db.SaveChangesAsync(ct);
        foreach (var it in r.Items)
        {
            if (it.PartId == Guid.Empty || it.Qty <= 0) throw new ValidationException("Validation failed", new[] { "Invalid item values." });
            _db.StockTransferItems.Add(new StockTransferItem { StockTransferId = t.Id, PartId = it.PartId, Qty = it.Qty });
        }
        await _db.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task<TransferResponse> RequestAsync(Guid actorUserId, Guid fromBranchId, Guid id, CancellationToken ct = default)
    {
        var t = await LoadRelevant(id, ct);
        if (t.FromBranchId != fromBranchId) throw new ForbiddenException("Wrong branch");
        if (t.Status != StockTransferStatus.Draft) throw new DomainException("Only Draft can be requested", 409);
        t.Status = StockTransferStatus.Requested;
        t.RequestedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task<TransferResponse> ShipAsync(Guid actorUserId, Guid fromBranchId, Guid id, CancellationToken ct = default)
    {
        var t = await LoadRelevant(id, ct);
        if (t.FromBranchId != fromBranchId) throw new ForbiddenException("Wrong branch");
        if (t.Status != StockTransferStatus.Requested) throw new DomainException("Only Requested can be shipped", 409);

        var items = await _db.StockTransferItems.AsNoTracking().Where(x => x.StockTransferId == t.Id && !x.IsDeleted).ToListAsync(ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var it in items)
        {
            var stock = await _db.PartStocks.FirstOrDefaultAsync(x => x.BranchId == t.FromBranchId && x.LocationId == t.FromLocationId && x.PartId == it.PartId && !x.IsDeleted, ct);
            if (stock is null || stock.QuantityOnHand < it.Qty) throw new DomainException("Insufficient stock", 409);
            stock.QuantityOnHand -= it.Qty;

            _db.StockLedgers.Add(new StockLedger
            {
                BranchId = t.FromBranchId,
                LocationId = t.FromLocationId,
                PartId = it.PartId,
                MovementType = StockMovementType.TransferOut,
                ReferenceType = "TRANSFER",
                ReferenceId = t.Id,
                QuantityDelta = -it.Qty,
                UnitCost = null,
                Notes = null,
                PerformedByUserId = actorUserId,
                PerformedAt = DateTimeOffset.UtcNow
            });
        }

        t.Status = StockTransferStatus.Shipped;
        t.ShippedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Map(t);
    }

    public async Task<TransferResponse> ReceiveAsync(Guid actorUserId, Guid toBranchId, Guid id, CancellationToken ct = default)
    {
        var t = await LoadRelevant(id, ct);
        if (t.ToBranchId != toBranchId) throw new ForbiddenException("Wrong branch");
        if (t.Status != StockTransferStatus.Shipped) throw new DomainException("Only Shipped can be received", 409);

        var items = await _db.StockTransferItems.AsNoTracking().Where(x => x.StockTransferId == t.Id && !x.IsDeleted).ToListAsync(ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var it in items)
        {
            var stock = await _db.PartStocks.FirstOrDefaultAsync(x => x.BranchId == t.ToBranchId && x.LocationId == t.ToLocationId && x.PartId == it.PartId && !x.IsDeleted, ct);
            if (stock is null)
            {
                stock = new PartStock { BranchId = t.ToBranchId, LocationId = t.ToLocationId, PartId = it.PartId, QuantityOnHand = 0m };
                _db.PartStocks.Add(stock);
                await _db.SaveChangesAsync(ct);
            }
            stock.QuantityOnHand += it.Qty;

            _db.StockLedgers.Add(new StockLedger
            {
                BranchId = t.ToBranchId,
                LocationId = t.ToLocationId,
                PartId = it.PartId,
                MovementType = StockMovementType.TransferIn,
                ReferenceType = "TRANSFER",
                ReferenceId = t.Id,
                QuantityDelta = it.Qty,
                UnitCost = null,
                Notes = null,
                PerformedByUserId = actorUserId,
                PerformedAt = DateTimeOffset.UtcNow
            });
        }

        t.Status = StockTransferStatus.Received;
        t.ReceivedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Map(t);
    }

    public async Task<PageResponse<TransferResponse>> GetPagedAsync(Guid branchId, PageRequest r, CancellationToken ct = default)
    {
        var q = _db.StockTransfers.AsNoTracking().Where(x => !x.IsDeleted && (x.FromBranchId == branchId || x.ToBranchId == branchId));
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderByDescending(x => x.CreatedAt).Skip((page-1)*size).Take(size)
            .Select(x => new TransferResponse(x.Id, x.TransferNo, x.Status, x.FromBranchId, x.FromLocationId, x.ToBranchId, x.ToLocationId, x.RequestedAt, x.ShippedAt, x.ReceivedAt, x.Notes))
            .ToListAsync(ct);
        return new PageResponse<TransferResponse>(items, total, page, size);
    }

    private async Task<StockTransfer> LoadRelevant(Guid id, CancellationToken ct)
    {
        var t = await _db.StockTransfers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t is null) throw new NotFoundException("Transfer not found");
        return t;
    }

    private static TransferResponse Map(StockTransfer x) => new(x.Id, x.TransferNo, x.Status, x.FromBranchId, x.FromLocationId, x.ToBranchId, x.ToLocationId, x.RequestedAt, x.ShippedAt, x.ReceivedAt, x.Notes);
}
