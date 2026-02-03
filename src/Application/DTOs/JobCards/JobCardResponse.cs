using Domain.Enums;
namespace Application.DTOs.JobCards;
public sealed record JobCardResponse(Guid Id,Guid BranchId,Guid CustomerId,Guid VehicleId,JobCardStatus Status,DateTimeOffset? EntryAt,DateTimeOffset? ExitAt,int? Mileage,string? InitialReport,string? Diagnosis);
