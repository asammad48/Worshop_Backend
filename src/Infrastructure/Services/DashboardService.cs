using Application.DTOs.Dashboard;
using Application.DTOs.Notifications;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardOverviewResponse> GetOverviewAsync(DashboardOverviewQuery query, UserRole role, Guid currentUserId)
    {
        var fromDate = query.From ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6));
        var toDate = query.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var tz = query.Tz ?? "Asia/Karachi";

        var fromOffset = GetStartOfDay(fromDate, tz);
        var toOffset = GetEndOfDay(toDate, tz);

        Guid? branchId = query.BranchId;
        if (role != UserRole.HQ_ADMIN)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentUserId);
            branchId = user?.BranchId;
        }

        var cards = new List<KpiCardDto>();
        var series = new List<ChartSeriesDto>();

        if (role == UserRole.HQ_ADMIN)
        {
            cards = await GetHqAdminCards(branchId, fromOffset, toOffset);
            series = await GetHqAdminSeries(branchId, fromDate, toDate, tz);
        }
        else if (role == UserRole.BRANCH_MANAGER)
        {
            cards = await GetManagerCards(branchId, fromOffset, toOffset);
            series = await GetManagerSeries(branchId, fromDate, toDate, tz);
        }
        else if (role == UserRole.STOREKEEPER)
        {
            cards = await GetStoreCards(branchId, fromOffset, toOffset);
            series = await GetStoreSeries(branchId, fromDate, toDate, tz);
        }
        else if (role == UserRole.CASHIER)
        {
            cards = await GetCashierCards(branchId, fromOffset, toOffset);
            series = await GetCashierSeries(branchId, fromDate, toDate, tz);
        }
        else if (role == UserRole.TECHNICIAN)
        {
            cards = await GetTechCards(branchId, currentUserId, fromOffset, toOffset);
            series = await GetTechSeries(branchId, currentUserId, fromDate, toDate, tz);
        }

        var alerts = await GetDashboardAlerts(role, branchId, currentUserId);
        var quickActions = GetQuickActions(role);

        return new DashboardOverviewResponse(
            role.ToString(),
            branchId,
            fromDate,
            toDate,
            cards,
            series,
            alerts,
            quickActions
        );
    }

    private static DateTimeOffset GetStartOfDay(DateOnly date, string tz)
    {
        var tzi = TimeZoneInfo.FindSystemTimeZoneById(tz);
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var offset = tzi.GetUtcOffset(dt);
        return new DateTimeOffset(dt, offset);
    }

    private static DateTimeOffset GetEndOfDay(DateOnly date, string tz)
    {
        var tzi = TimeZoneInfo.FindSystemTimeZoneById(tz);
        var dt = date.ToDateTime(TimeOnly.MaxValue);
        var offset = tzi.GetUtcOffset(dt);
        return new DateTimeOffset(dt, offset);
    }

    private async Task<List<KpiCardDto>> GetHqAdminCards(Guid? branchId, DateTimeOffset from, DateTimeOffset to)
    {
        var jobCardsQuery = _db.JobCards.AsNoTracking().Where(x => !x.IsDeleted);
        if (branchId.HasValue) jobCardsQuery = jobCardsQuery.Where(x => x.BranchId == branchId.Value);

        var openJobCards = await jobCardsQuery.CountAsync(x => x.Status != JobCardStatus.Pagado);

        var paymentsQuery = _db.Payments.AsNoTracking().Where(x => !x.IsDeleted && x.PaidAt >= from && x.PaidAt <= to);
        if (branchId.HasValue) paymentsQuery = paymentsQuery.Where(x => _db.Invoices.Any(i => i.Id == x.InvoiceId && _db.JobCards.Any(j => j.Id == i.JobCardId && j.BranchId == branchId.Value)));
        var revenue = await paymentsQuery.SumAsync(x => x.Amount);

        var expensesQuery = _db.Expenses.AsNoTracking().Where(x => !x.IsDeleted && x.ExpenseAt >= from && x.ExpenseAt <= to);
        if (branchId.HasValue) expensesQuery = expensesQuery.Where(x => x.BranchId == branchId.Value);
        var expenses = await expensesQuery.SumAsync(x => x.Amount);

        var wagesQuery = _db.WagePayments.AsNoTracking().Where(x => !x.IsDeleted && x.PaidAt >= from && x.PaidAt <= to);
        if (branchId.HasValue) wagesQuery = wagesQuery.Where(x => x.BranchId == branchId.Value);
        var wages = await wagesQuery.SumAsync(x => x.Amount);

        var lowStock = await _db.PartStocks.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId.Value))
            .CountAsync(x => x.QuantityOnHand < x.Part!.ReorderLevel);

        var pendingApprovals = await _db.Approvals.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == ApprovalStatus.Pending && (branchId == null || x.BranchId == branchId.Value))
            .CountAsync();

        return new List<KpiCardDto>
        {
            new("total_jobcards_open", "Open Job Cards", openJobCards),
            new("revenue_collected", "Revenue", revenue, "PKR"),
            new("expenses_total", "Expenses", expenses, "PKR"),
            new("wages_total", "Wages", wages, "PKR"),
            new("low_stock_items", "Low Stock", lowStock),
            new("approvals_pending", "Pending Approvals", pendingApprovals)
        };
    }

    private async Task<List<ChartSeriesDto>> GetHqAdminSeries(Guid? branchId, DateOnly from, DateOnly to, string tz)
    {
        var dates = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(from.AddDays)
            .ToList();

        // Created Series
        var createdPoints = await _db.JobCards.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId.Value))
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Closed Series (Status = Pagado)
        var closedPoints = await _db.JobCards.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == JobCardStatus.Pagado && (branchId == null || x.BranchId == branchId.Value))
            .GroupBy(x => x.ExitAt != null ? x.ExitAt.Value.Date : x.UpdatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // Revenue Series
        var revenuePoints = await _db.Payments.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || _db.Invoices.Any(i => i.Id == x.InvoiceId && _db.JobCards.Any(j => j.Id == i.JobCardId && j.BranchId == branchId.Value))))
            .GroupBy(x => x.PaidAt.Date)
            .Select(g => new { Date = g.Key, Sum = g.Sum(p => p.Amount) })
            .ToListAsync();

        return new List<ChartSeriesDto>
        {
            new("jobcards_created", "Created", dates.Select(d => new ChartPointDto(d, (decimal)(createdPoints.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList()),
            new("jobcards_closed", "Closed", dates.Select(d => new ChartPointDto(d, (decimal)(closedPoints.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList()),
            new("revenue_collected", "Revenue", dates.Select(d => new ChartPointDto(d, revenuePoints.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Sum ?? 0)).ToList())
        };
    }

    private async Task<List<KpiCardDto>> GetManagerCards(Guid? branchId, DateTimeOffset from, DateTimeOffset to)
    {
        if (!branchId.HasValue) return new List<KpiCardDto>();

        var inShop = await _db.JobCards.CountAsync(x => !x.IsDeleted && x.BranchId == branchId && x.Status != JobCardStatus.Pagado);

        var avgDays = 0m;
        var closedInPeriod = await _db.JobCards
            .Where(x => !x.IsDeleted && x.BranchId == branchId && x.Status == JobCardStatus.Pagado && x.ExitAt >= from && x.ExitAt <= to)
            .ToListAsync();
        if (closedInPeriod.Any())
        {
            avgDays = (decimal)closedInPeriod.Average(x => (x.ExitAt!.Value - (x.EntryAt ?? x.CreatedAt)).TotalDays);
        }

        var roadblockers = await _db.Roadblockers.CountAsync(x => !x.IsDeleted && !x.IsResolved && _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId));

        var overdueTasks = await _db.JobTasks.CountAsync(x => !x.IsDeleted && x.Status != JobTaskStatus.Done && _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId) && x.CreatedAt.AddDays(1) < DateTimeOffset.UtcNow); // Assuming 1 day is overdue limit if not specified

        var partsWaiting = await _db.JobPartRequests.CountAsync(x => !x.IsDeleted && x.BranchId == branchId && x.Status == JobPartRequestStatus.Requested);

        var collectionsToday = await _db.Payments
            .Where(x => !x.IsDeleted && x.PaidAt >= DateTimeOffset.UtcNow.Date && _db.Invoices.Any(i => i.Id == x.InvoiceId && _db.JobCards.Any(j => j.Id == i.JobCardId && j.BranchId == branchId)))
            .SumAsync(x => x.Amount);

        return new List<KpiCardDto>
        {
            new("jobcards_in_shop", "In Shop", inShop),
            new("avg_days_in_shop", "Avg Days", Math.Round(avgDays, 1), "Days"),
            new("roadblockers_open", "Roadblockers", roadblockers),
            new("tasks_overdue", "Tasks Overdue", overdueTasks),
            new("parts_waiting", "Parts Waiting", partsWaiting),
            new("collections_today", "Collections Today", collectionsToday, "PKR")
        };
    }

    private async Task<List<ChartSeriesDto>> GetManagerSeries(Guid? branchId, DateOnly from, DateOnly to, string tz)
    {
        if (!branchId.HasValue) return new List<ChartSeriesDto>();
        var dates = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(from.AddDays).ToList();

        var inShopPoints = await _db.JobCards.AsNoTracking()
            .Where(x => !x.IsDeleted && x.BranchId == branchId && x.Status != JobCardStatus.Pagado)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var tasksCompleted = await _db.JobTasks.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == JobTaskStatus.Done && _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId))
            .GroupBy(x => x.EndedAt != null ? x.EndedAt.Value.Date : x.UpdatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return new List<ChartSeriesDto>
        {
            new("jobcards_in_shop", "In Shop", dates.Select(d => new ChartPointDto(d, (decimal)(inShopPoints.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList()),
            new("tasks_completed", "Tasks Completed", dates.Select(d => new ChartPointDto(d, (decimal)(tasksCompleted.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList())
        };
    }

    private async Task<List<KpiCardDto>> GetStoreCards(Guid? branchId, DateTimeOffset from, DateTimeOffset to)
    {
        var lowStock = await _db.PartStocks.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId.Value))
            .CountAsync(x => x.QuantityOnHand < x.Part!.ReorderLevel);

        var pendingPo = await _db.PurchaseOrders.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status != PurchaseOrderStatus.Received && (branchId == null || x.BranchId == branchId.Value))
            .CountAsync();

        var pendingTransfers = await _db.StockTransfers.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status != StockTransferStatus.Received && (branchId == null || x.FromBranchId == branchId.Value || x.ToBranchId == branchId.Value))
            .CountAsync();

        var pendingRequests = await _db.JobPartRequests.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == JobPartRequestStatus.Requested && (branchId == null || x.BranchId == branchId.Value))
            .CountAsync();

        return new List<KpiCardDto>
        {
            new("low_stock_items", "Low Stock", lowStock),
            new("pending_po", "Pending PO", pendingPo),
            new("pending_transfers", "Pending Transfers", pendingTransfers),
            new("part_requests_pending", "Part Requests", pendingRequests)
        };
    }

    private async Task<List<ChartSeriesDto>> GetStoreSeries(Guid? branchId, DateOnly from, DateOnly to, string tz)
    {
        var dates = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(from.AddDays).ToList();

        var adjCount = await _db.StockAdjustments.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId.Value))
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var poReceived = await _db.PurchaseOrders.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == PurchaseOrderStatus.Received && (branchId == null || x.BranchId == branchId.Value))
            .GroupBy(x => x.ReceivedAt != null ? x.ReceivedAt.Value.Date : x.UpdatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return new List<ChartSeriesDto>
        {
            new("stock_adjustments_count", "Adjustments", dates.Select(d => new ChartPointDto(d, (decimal)(adjCount.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList()),
            new("po_received_count", "PO Received", dates.Select(d => new ChartPointDto(d, (decimal)(poReceived.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList())
        };
    }

    private async Task<List<KpiCardDto>> GetCashierCards(Guid? branchId, DateTimeOffset from, DateTimeOffset to)
    {
        var invoicesDue = await _db.Invoices.AsNoTracking()
            .Where(x => !x.IsDeleted && x.PaymentStatus != PaymentStatus.Paid && (branchId == null || _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId.Value)))
            .CountAsync();

        var dueAmount = await _db.Invoices.AsNoTracking()
            .Where(x => !x.IsDeleted && x.PaymentStatus != PaymentStatus.Paid && (branchId == null || _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId.Value)))
            .SumAsync(x => x.Total);

        var pendingApprovals = await _db.JobCards.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == JobCardStatus.EsperandoAprobacion && (branchId == null || x.BranchId == branchId.Value))
            .CountAsync();

        var paymentsToday = await _db.Payments.AsNoTracking()
            .Where(x => !x.IsDeleted && x.PaidAt >= DateTimeOffset.UtcNow.Date && (branchId == null || _db.Invoices.Any(i => i.Id == x.InvoiceId && _db.JobCards.Any(j => j.Id == i.JobCardId && j.BranchId == branchId.Value))))
            .SumAsync(x => x.Amount);

        return new List<KpiCardDto>
        {
            new("invoices_due_count", "Due Invoices", invoicesDue),
            new("due_amount_total", "Total Due", dueAmount, "PKR"),
            new("approvals_pending_cashier", "Approvals Pending", pendingApprovals),
            new("payments_today", "Payments Today", paymentsToday, "PKR")
        };
    }

    private async Task<List<ChartSeriesDto>> GetCashierSeries(Guid? branchId, DateOnly from, DateOnly to, string tz)
    {
        var dates = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(from.AddDays).ToList();

        var payments = await _db.Payments.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || _db.Invoices.Any(i => i.Id == x.InvoiceId && _db.JobCards.Any(j => j.Id == i.JobCardId && j.BranchId == branchId.Value))))
            .GroupBy(x => x.PaidAt.Date)
            .Select(g => new { Date = g.Key, Sum = g.Sum(p => p.Amount) })
            .ToListAsync();

        return new List<ChartSeriesDto>
        {
            new("payments_collected", "Collections", dates.Select(d => new ChartPointDto(d, payments.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Sum ?? 0)).ToList())
        };
    }

    private async Task<List<KpiCardDto>> GetTechCards(Guid? branchId, Guid userId, DateTimeOffset from, DateTimeOffset to)
    {
        var myTasksOpen = await _db.JobTasks.CountAsync(x => !x.IsDeleted && x.Status != JobTaskStatus.Done && x.StartedByUserId == userId);
        var myTasksOverdue = await _db.JobTasks.CountAsync(x => !x.IsDeleted && x.Status != JobTaskStatus.Done && x.StartedByUserId == userId && x.CreatedAt.AddDays(1) < DateTimeOffset.UtcNow);
        var myJobCards = await _db.JobCards.CountAsync(x => !x.IsDeleted && x.Status != JobCardStatus.Pagado && _db.JobTasks.Any(t => t.JobCardId == x.Id && t.StartedByUserId == userId));
        var myRoadblockers = await _db.Roadblockers.CountAsync(x => !x.IsDeleted && !x.IsResolved && x.CreatedByUserId == userId);

        return new List<KpiCardDto>
        {
            new("my_tasks_open", "My Open Tasks", myTasksOpen),
            new("my_tasks_overdue", "Overdue Tasks", myTasksOverdue),
            new("my_jobcards_active", "My Job Cards", myJobCards),
            new("my_roadblockers_open", "My Roadblockers", myRoadblockers)
        };
    }

    private async Task<List<ChartSeriesDto>> GetTechSeries(Guid? branchId, Guid userId, DateOnly from, DateOnly to, string tz)
    {
        var dates = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(from.AddDays).ToList();

        var tasksCompleted = await _db.JobTasks.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == JobTaskStatus.Done && x.EndedByUserId == userId)
            .GroupBy(x => x.EndedAt != null ? x.EndedAt.Value.Date : x.UpdatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return new List<ChartSeriesDto>
        {
            new("tasks_completed", "Completed Tasks", dates.Select(d => new ChartPointDto(d, (decimal)(tasksCompleted.FirstOrDefault(p => DateOnly.FromDateTime(p.Date) == d)?.Count ?? 0))).ToList())
        };
    }

    private async Task<DashboardAlertsDto> GetDashboardAlerts(UserRole role, Guid? branchId, Guid userId)
    {
        var jobCardsOverdue = await _db.JobCards.CountAsync(x => !x.IsDeleted && x.Status != JobCardStatus.Pagado && (branchId == null || x.BranchId == branchId) && x.CreatedAt.AddDays(3) < DateTimeOffset.UtcNow);
        var roadblockersOpen = await _db.Roadblockers.CountAsync(x => !x.IsDeleted && !x.IsResolved && (branchId == null || _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId)));
        var approvalsPending = await _db.Approvals.CountAsync(x => !x.IsDeleted && x.Status == ApprovalStatus.Pending && (branchId == null || x.BranchId == branchId));
        var lowStock = await _db.PartStocks.CountAsync(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId) && x.QuantityOnHand < x.Part!.ReorderLevel);
        var pendingPo = await _db.PurchaseOrders.CountAsync(x => !x.IsDeleted && x.Status != PurchaseOrderStatus.Received && (branchId == null || x.BranchId == branchId));
        var pendingTransfer = await _db.StockTransfers.CountAsync(x => !x.IsDeleted && x.Status != StockTransferStatus.Received && (branchId == null || x.FromBranchId == branchId || x.ToBranchId == branchId));
        var tasksOverdue = await _db.JobTasks.CountAsync(x => !x.IsDeleted && x.Status != JobTaskStatus.Done && (branchId == null || _db.JobCards.Any(j => j.Id == x.JobCardId && j.BranchId == branchId)) && x.CreatedAt.AddDays(1) < DateTimeOffset.UtcNow);

        var topJobCards = await GetJobCardAlertsInternal(branchId, null, null, null, null, 1, 5);

        var topNotifications = await _db.Notifications.AsNoTracking()
            .Where(x => !x.IsDeleted && (x.UserId == userId || (x.UserId == null && x.BranchId == branchId)) && !x.IsRead)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new NotificationResponse(x.Id, x.Type, x.Title, x.Message, x.RefType, x.RefId, x.IsRead, x.CreatedAt))
            .ToListAsync();

        return new DashboardAlertsDto(
            jobCardsOverdue,
            roadblockersOpen,
            approvalsPending,
            lowStock,
            pendingPo,
            pendingTransfer,
            tasksOverdue,
            topJobCards.Items,
            topNotifications
        );
    }

    private DashboardQuickActionsDto GetQuickActions(UserRole role)
    {
        return new DashboardQuickActionsDto(
            CanCreateJobCard: role is UserRole.HQ_ADMIN or UserRole.BRANCH_MANAGER or UserRole.RECEPTIONIST,
            CanCreatePurchaseOrder: role is UserRole.HQ_ADMIN or UserRole.BRANCH_MANAGER or UserRole.STOREKEEPER,
            CanCreateTransfer: role is UserRole.HQ_ADMIN or UserRole.BRANCH_MANAGER or UserRole.STOREKEEPER,
            CanCreateInvoice: role is UserRole.HQ_ADMIN or UserRole.BRANCH_MANAGER or UserRole.CASHIER,
            CanMarkAttendance: true
        );
    }

    public async Task<PageResponse<JobCardAlertRow>> GetJobCardAlertsAsync(Guid? branchId, string? status, int? minDaysInShop, bool? hasRoadblocker, ApprovalRole? requiresApprovalRole, int pageNumber, int pageSize)
    {
        return await GetJobCardAlertsInternal(branchId, status, minDaysInShop, hasRoadblocker, requiresApprovalRole, pageNumber, pageSize);
    }

    private async Task<PageResponse<JobCardAlertRow>> GetJobCardAlertsInternal(Guid? branchId, string? status, int? minDaysInShop, bool? hasRoadblocker, ApprovalRole? requiresApprovalRole, int pageNumber, int pageSize)
    {
        var query = _db.JobCards.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .Where(x => !x.IsDeleted);

        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<JobCardStatus>(status, true, out var s))
                query = query.Where(x => x.Status == s);
        }

        if (hasRoadblocker.HasValue)
        {
            if (hasRoadblocker.Value)
                query = query.Where(x => _db.Roadblockers.Any(r => r.JobCardId == x.Id && !r.IsResolved));
            else
                query = query.Where(x => !_db.Roadblockers.Any(r => r.JobCardId == x.Id && !r.IsResolved));
        }

        if (requiresApprovalRole.HasValue)
        {
             query = query.Where(x => x.Status == JobCardStatus.EsperandoAprobacion);
        }

        if (minDaysInShop.HasValue)
        {
            var threshold = DateTimeOffset.UtcNow.AddDays(-minDaysInShop.Value);
            query = query.Where(x => (x.EntryAt ?? x.CreatedAt) < threshold);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new JobCardAlertRow(
                x.Id,
                x.Vehicle != null ? x.Vehicle.Plate : "",
                x.Customer != null ? x.Customer.FullName : "",
                x.Status.ToString(),
                x.EntryAt,
                x.ExitAt,
                (int)(DateTimeOffset.UtcNow - (x.EntryAt ?? x.CreatedAt)).TotalDays,
                _db.Roadblockers.Any(r => r.JobCardId == x.Id && !r.IsResolved),
                _db.Roadblockers.Where(r => r.JobCardId == x.Id && !r.IsResolved).Select(r => r.Description).FirstOrDefault(),
                x.Status == JobCardStatus.EsperandoAprobacion,
                x.Status == JobCardStatus.EsperandoAprobacion ? "Supervisor" : null,
                _db.Invoices.Where(i => i.JobCardId == x.Id).Select(i => i.Total).FirstOrDefault(),
                _db.Invoices.Where(i => i.JobCardId == x.Id).Select(i => i.Total - _db.Payments.Where(p => p.InvoiceId == i.Id).Sum(p => p.Amount)).FirstOrDefault()
            ))
            .ToListAsync();

        return new PageResponse<JobCardAlertRow>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<InventoryDashboardResponse> GetInventoryDashboardAsync(Guid? branchId, bool belowReorderOnly, bool? pendingPoOnly, bool? pendingTransfersOnly, int pageNumber, int pageSize)
    {
        var lowStockQuery = _db.PartStocks.AsNoTracking()
            .Include(x => x.Part)
            .Include(x => x.Location)
            .Where(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId.Value));

        if (belowReorderOnly)
            lowStockQuery = lowStockQuery.Where(x => x.QuantityOnHand < x.Part!.ReorderLevel);

        var lowStockCount = await lowStockQuery.CountAsync();
        var lowStockTop = await lowStockQuery
            .OrderBy(x => x.QuantityOnHand)
            .Take(pageSize)
            .Select(x => new LowStockRow(x.PartId, x.Part!.Sku, x.Part.Name, x.Location!.Name, x.QuantityOnHand, x.Part.ReorderLevel))
            .ToListAsync();

        var poQuery = _db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted && (branchId == null || x.BranchId == branchId.Value));

        if (pendingPoOnly == true)
            poQuery = poQuery.Where(x => x.Status != PurchaseOrderStatus.Received);

        var poCount = await poQuery.CountAsync();
        var poTop = await poQuery
            .OrderByDescending(x => x.OrderedAt)
            .Take(pageSize)
            .Select(x => new PurchaseOrderRow(x.Id, x.OrderNo, x.Supplier!.Name, x.Status.ToString(), x.OrderedAt ?? x.CreatedAt, (int)(DateTimeOffset.UtcNow - (x.OrderedAt ?? x.CreatedAt)).TotalDays))
            .ToListAsync();

        var transferQuery = _db.StockTransfers.AsNoTracking()
            .Where(x => !x.IsDeleted && (branchId == null || x.FromBranchId == branchId.Value || x.ToBranchId == branchId.Value));

        if (pendingTransfersOnly == true)
            transferQuery = transferQuery.Where(x => x.Status != StockTransferStatus.Received);

        var transferCount = await transferQuery.CountAsync();
        var transferTop = await transferQuery
            .OrderByDescending(x => x.RequestedAt)
            .Take(pageSize)
            .Select(x => new TransferRow(
                x.Id,
                x.TransferNo,
                x.Status.ToString(),
                _db.Branches.Where(b => b.Id == x.FromBranchId).Select(b => b.Name).FirstOrDefault() ?? "",
                _db.Branches.Where(b => b.Id == x.ToBranchId).Select(b => b.Name).FirstOrDefault() ?? "",
                x.RequestedAt ?? x.CreatedAt,
                (int)(DateTimeOffset.UtcNow - (x.RequestedAt ?? x.CreatedAt)).TotalDays))
            .ToListAsync();

        return new InventoryDashboardResponse(branchId, lowStockCount, poCount, transferCount, lowStockTop, poTop, transferTop);
    }

    public async Task<PageResponse<EmployeeKpiRow>> GetEmployeeKpisAsync(Guid? branchId, DateOnly? from, DateOnly? to, Guid? employeeUserId, int pageNumber, int pageSize)
    {
        var query = _db.Users.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Role == UserRole.TECHNICIAN);

        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);
        if (employeeUserId.HasValue) query = query.Where(x => x.Id == employeeUserId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(emp => new EmployeeKpiRow(
                emp.Id,
                emp.Email,
                _db.JobTasks.Count(t => t.EndedByUserId == emp.Id && t.Status == JobTaskStatus.Done),
                _db.JobTasks.Count(t => t.StartedByUserId == emp.Id && t.Status != JobTaskStatus.Done && t.CreatedAt.AddDays(1) < DateTimeOffset.UtcNow),
                (decimal)(_db.JobTasks.Where(t => t.EndedByUserId == emp.Id && t.Status == JobTaskStatus.Done).Average(t => (double?)t.TotalMinutes) ?? 0),
                _db.JobCards.Count(j => _db.JobTasks.Any(t => t.JobCardId == j.Id && t.StartedByUserId == emp.Id)),
                0,
                _db.AttendanceRecords.Count(a => a.EmployeeUserId == emp.Id && a.Status == AttendanceStatus.Present),
                _db.AttendanceRecords.Count(a => a.EmployeeUserId == emp.Id && a.Status == AttendanceStatus.Late)
            ))
            .ToListAsync();

        return new PageResponse<EmployeeKpiRow>(items, totalCount, pageNumber, pageSize);
    }
}
