using Application.DTOs.Notifications;

namespace Application.DTOs.Dashboard;

public sealed record DashboardOverviewQuery(
    Guid? BranchId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Tz = "Asia/Karachi"
);

public sealed record KpiCardDto(
    string Key,
    string Title,
    decimal Value,
    string? Unit = null,
    decimal? DeltaValue = null,
    decimal? DeltaPercent = null,
    string? Trend = null // up|down|flat
);

public sealed record ChartPointDto(
    DateOnly Date,
    decimal Value
);

public sealed record ChartSeriesDto(
    string Key,
    string Label,
    IReadOnlyList<ChartPointDto> Points
);

public sealed record DashboardOverviewResponse(
    string Role,
    Guid? BranchId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<KpiCardDto> Cards,
    IReadOnlyList<ChartSeriesDto> Series,
    DashboardAlertsDto Alerts,
    DashboardQuickActionsDto QuickActions
);

public sealed record DashboardAlertsDto(
    int JobCardsOverdueCount,
    int RoadblockersOpenCount,
    int ApprovalsPendingCount,
    int LowStockCount,
    int PendingPoCount,
    int PendingTransferCount,
    int TasksOverdueCount,
    IReadOnlyList<JobCardAlertRow> TopJobCardAlerts,   // max 5
    IReadOnlyList<NotificationResponse> TopNotifications // max 5
);

public sealed record DashboardQuickActionsDto(
    bool CanCreateJobCard,
    bool CanCreatePurchaseOrder,
    bool CanCreateTransfer,
    bool CanCreateInvoice,
    bool CanMarkAttendance
);
