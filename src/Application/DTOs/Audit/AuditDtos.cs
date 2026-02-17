namespace Application.DTOs.Audit;

public sealed record AuditLogResponse(Guid Id, Guid? BranchId, string Action, string EntityType, Guid EntityId, string? OldValue, string? NewValue, Guid PerformedByUserId, DateTimeOffset PerformedAt, string? ActorEmail = null, string? BranchName = null);
