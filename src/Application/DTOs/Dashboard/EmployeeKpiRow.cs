namespace Application.DTOs.Dashboard;

public sealed record EmployeeKpiRow(
    Guid EmployeeUserId,
    string EmployeeEmail,
    decimal TasksCompleted,
    decimal TasksOverdue,
    decimal AvgTaskMinutes,
    decimal JobCardsHandled,
    decimal ComebackRatePercent,
    decimal AttendancePresentDays,
    decimal AttendanceLateCount
);
