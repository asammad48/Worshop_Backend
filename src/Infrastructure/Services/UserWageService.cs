using Application.DTOs.Wages;
using Application.Services.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class UserWageService : IUserWageService
{
    private readonly AppDbContext _db;
    private readonly IInvoiceRecomputeQueue _recomputeQueue;

    public UserWageService(AppDbContext db, IInvoiceRecomputeQueue recomputeQueue)
    {
        _db = db;
        _recomputeQueue = recomputeQueue;
    }

    public async Task<UserWageResponse> GetLatestWageAsync(Guid userId, CancellationToken ct = default)
    {
        var wage = await _db.UserWages
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.EffectiveTo == null) // Prefer non-ended
            .ThenByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (wage == null) throw new NotFoundException("Wage not found for user");

        return Map(wage);
    }

    public async Task<UserWageResponse> UpsertWageAsync(Guid userId, UserWageUpsertRequest req, CancellationToken ct = default)
    {
        var userExists = await _db.Users.AnyAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (!userExists) throw new NotFoundException("User not found");

        if (req.HourlyRate <= 0) throw new ValidationException("Validation failed", new[] { "Hourly rate must be > 0" });
        if (req.Currency != null && req.Currency.Length != 3) throw new ValidationException("Validation failed", new[] { "Currency must be exactly 3 characters" });
        if (req.EffectiveFrom.HasValue && req.EffectiveTo.HasValue && req.EffectiveFrom > req.EffectiveTo)
            throw new ValidationException("Validation failed", new[] { "EffectiveFrom must be <= EffectiveTo" });

        var wage = new UserWage
        {
            UserId = userId,
            HourlyRate = req.HourlyRate,
            Currency = (req.Currency ?? "PKR").ToUpperInvariant(),
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo
        };

        _db.UserWages.Add(wage);
        await _db.SaveChangesAsync(ct);

        // Enqueue recompute for all invoices this user worked on
        var jobCardIds = await _db.JobCardTimeLogs
            .Where(x => x.TechnicianUserId == userId && !x.IsDeleted)
            .Select(x => x.JobCardId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var jobCardId in jobCardIds)
        {
            await _recomputeQueue.EnqueueAsync(jobCardId, "Wage rate updated", ct);
        }

        return Map(wage);
    }

    private static UserWageResponse Map(UserWage w) =>
        new(w.UserId, w.HourlyRate, w.Currency, w.EffectiveFrom, w.EffectiveTo, w.UpdatedAt);
}
