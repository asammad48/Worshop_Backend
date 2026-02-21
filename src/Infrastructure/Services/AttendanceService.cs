using Application.DTOs.Attendance;
using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;

    public AttendanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CheckInAsync(Guid userId, Guid? branchId, AttendanceCheckInRequest req, CancellationToken ct = default)
    {
        var occurredAt = req.Timestamp ?? DateTimeOffset.UtcNow;
        var date = occurredAt.Date;

        var alreadyCheckedIn = await _db.AttendanceEvents.AnyAsync(x =>
            x.UserId == userId &&
            x.EventType == AttendanceEventType.CheckIn &&
            x.OccurredAt.Date == date &&
            !x.IsDeleted, ct);

        if (alreadyCheckedIn) throw new ValidationException("Validation failed", new[] { "Already checked in today" });

        var @event = new AttendanceEvent
        {
            UserId = userId,
            BranchId = branchId,
            EventType = AttendanceEventType.CheckIn,
            OccurredAt = occurredAt,
            Note = req.Note?.Trim()
        };

        _db.AttendanceEvents.Add(@event);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CheckOutAsync(Guid userId, AttendanceCheckOutRequest req, CancellationToken ct = default)
    {
        var occurredAt = req.Timestamp ?? DateTimeOffset.UtcNow;
        var date = occurredAt.Date;

        var alreadyCheckedOut = await _db.AttendanceEvents.AnyAsync(x =>
            x.UserId == userId &&
            x.EventType == AttendanceEventType.CheckOut &&
            x.OccurredAt.Date == date &&
            !x.IsDeleted, ct);

        if (alreadyCheckedOut) throw new ValidationException("Validation failed", new[] { "Already checked out today" });

        var checkIn = await _db.AttendanceEvents
            .Where(x => x.UserId == userId && x.EventType == AttendanceEventType.CheckIn && x.OccurredAt.Date == date && !x.IsDeleted)
            .OrderByDescending(x => x.OccurredAt)
            .FirstOrDefaultAsync(ct);

        if (checkIn == null) throw new ValidationException("Validation failed", new[] { "No check-in found for today" });
        if (occurredAt < checkIn.OccurredAt) throw new ValidationException("Validation failed", new[] { "Check-out cannot be before check-in" });

        var @event = new AttendanceEvent
        {
            UserId = userId,
            BranchId = checkIn.BranchId,
            EventType = AttendanceEventType.CheckOut,
            OccurredAt = occurredAt,
            Note = req.Note?.Trim()
        };

        _db.AttendanceEvents.Add(@event);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AttendanceMonthResponse> GetMyMonthAsync(Guid userId, int year, int month, CancellationToken ct = default)
    {
        return await GetUserMonthAsync(userId, year, month, ct);
    }

    public async Task<IReadOnlyList<AttendanceDaySummaryResponse>> GetTodayAttendanceAsync(Guid? branchId, CancellationToken ct = default)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var events = await _db.AttendanceEvents
            .Where(x => x.OccurredAt.Date == today && !x.IsDeleted)
            .Where(x => branchId == null || x.BranchId == branchId)
            .ToListAsync(ct);

        var summaries = events.GroupBy(x => x.UserId)
            .Select(g =>
            {
                var checkIn = g.FirstOrDefault(x => x.EventType == AttendanceEventType.CheckIn);
                var checkOut = g.FirstOrDefault(x => x.EventType == AttendanceEventType.CheckOut);
                var minutes = 0;
                if (checkIn != null && checkOut != null)
                {
                    minutes = (int)(checkOut.OccurredAt - checkIn.OccurredAt).TotalMinutes;
                }
                return new AttendanceDaySummaryResponse(
                    g.Key,
                    today,
                    checkIn?.OccurredAt,
                    checkOut?.OccurredAt,
                    minutes,
                    string.Join("; ", g.Select(x => x.Note).Where(x => !string.IsNullOrEmpty(x)))
                );
            }).ToList();

        return summaries;
    }

    public async Task<AttendanceMonthResponse> GetUserMonthAsync(Guid userId, int year, int month, CancellationToken ct = default)
    {
        var startDate = new DateTimeOffset(new DateTime(year, month, 1), TimeSpan.Zero);
        var endDate = startDate.AddMonths(1);

        var events = await _db.AttendanceEvents
            .Where(x => x.UserId == userId && x.OccurredAt >= startDate && x.OccurredAt < endDate && !x.IsDeleted)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync(ct);

        var days = events.GroupBy(x => x.OccurredAt.Date)
            .Select(g =>
            {
                var checkIn = g.FirstOrDefault(x => x.EventType == AttendanceEventType.CheckIn);
                var checkOut = g.FirstOrDefault(x => x.EventType == AttendanceEventType.CheckOut);
                var minutes = 0;
                if (checkIn != null && checkOut != null)
                {
                    minutes = (int)(checkOut.OccurredAt - checkIn.OccurredAt).TotalMinutes;
                }
                return new AttendanceDaySummaryResponse(
                    userId,
                    g.Key,
                    checkIn?.OccurredAt,
                    checkOut?.OccurredAt,
                    minutes,
                    string.Join("; ", g.Select(x => x.Note).Where(x => !string.IsNullOrEmpty(x)))
                );
            }).OrderBy(x => x.Date).ToList();

        var totalMinutes = days.Sum(x => x.TotalMinutes);
        var totalHours = Math.Round(totalMinutes / 60m, 2);

        return new AttendanceMonthResponse(userId, month, year, days, totalMinutes, totalHours);
    }
}
