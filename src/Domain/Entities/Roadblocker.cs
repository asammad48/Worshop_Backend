using Domain.Enums;
namespace Domain.Entities;
public sealed class Roadblocker: BaseEntity{ public Guid JobCardId{get;set;} public RoadblockerType Type{get;set;} public string? Description{get;set;} public bool IsResolved{get;set;} public DateTimeOffset CreatedAtLocal{get;set;} public DateTimeOffset? ResolvedAt{get;set;} public Guid CreatedByUserId{get;set;} }
