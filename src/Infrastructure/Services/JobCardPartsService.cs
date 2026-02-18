using Application.DTOs.JobCards;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class JobCardPartsService : IJobCardPartsService
{
    private readonly AppDbContext _db;
    public JobCardPartsService(AppDbContext db) { _db = db; }

    public async Task<JobCardPartUsageResponse> UsePartAsync(Guid actorUserId, Guid branchId, Guid jobCardId, JobCardPartUseRequest req, CancellationToken ct = default)
    {
        if (req.QuantityUsed <= 0) throw new ValidationException("Validation failed", new[] { "QuantityUsed must be > 0." });

        var job = await _db.JobCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (job is null) throw new NotFoundException("Job card not found");

        // ensure stock exists
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var stock = await _db.PartStocks.FirstOrDefaultAsync(x => x.BranchId == branchId && x.LocationId == req.LocationId && x.PartId == req.PartId && !x.IsDeleted, ct);
        if (stock is null || stock.QuantityOnHand < req.QuantityUsed) throw new DomainException("Insufficient stock", 409);

        stock.QuantityOnHand -= req.QuantityUsed;

        var usage = new JobCardPartUsage
        {
            JobCardId = jobCardId,
            PartId = req.PartId,
            LocationId = req.LocationId,
            QuantityUsed = req.QuantityUsed,
            UnitPrice = req.UnitPrice,
            UsedAt = DateTimeOffset.UtcNow,
            PerformedByUserId = actorUserId,
            CreatedBy = actorUserId
        };
        _db.JobCardPartUsages.Add(usage);

        _db.StockLedgers.Add(new StockLedger
        {
            BranchId = branchId,
            LocationId = req.LocationId,
            PartId = req.PartId,
            MovementType = StockMovementType.Consumption,
            ReferenceType = "JOBCARD",
            ReferenceId = jobCardId,
            QuantityDelta = -req.QuantityUsed,
            UnitCost = null,
            Notes = req.Notes?.Trim(),
            PerformedByUserId = actorUserId,
            PerformedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdInternalAsync(usage.Id, ct);
    }

    public async Task<IReadOnlyList<JobCardPartUsageResponse>> ListAsync(Guid branchId, Guid jobCardId, CancellationToken ct = default)
    {
        var jobExists = await _db.JobCards.AnyAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);
        if (!jobExists) throw new NotFoundException("Job card not found");

        return await (from u in _db.JobCardPartUsages.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                      join part in _db.Parts on u.PartId equals part.Id
                      join loc in _db.Locations on u.LocationId equals loc.Id
                      orderby u.UsedAt descending
                      select new JobCardPartUsageResponse(
                          u.Id, u.JobCardId, u.LocationId, u.PartId, u.QuantityUsed, u.UnitPrice, u.UsedAt, u.PerformedByUserId,
                          part.Sku, part.Name, loc.Code, loc.Name
                      )).ToListAsync(ct);
    }

    private async Task<JobCardPartUsageResponse> GetByIdInternalAsync(Guid id, CancellationToken ct)
    {
        var usage = await (from u in _db.JobCardPartUsages.Where(x => x.Id == id && !x.IsDeleted)
                           join part in _db.Parts on u.PartId equals part.Id
                           join loc in _db.Locations on u.LocationId equals loc.Id
                           select new JobCardPartUsageResponse(
                               u.Id, u.JobCardId, u.LocationId, u.PartId, u.QuantityUsed, u.UnitPrice, u.UsedAt, u.PerformedByUserId,
                               part.Sku, part.Name, loc.Code, loc.Name
                           )).FirstOrDefaultAsync(ct);

        return usage ?? throw new NotFoundException("Part usage not found");
    }
}
