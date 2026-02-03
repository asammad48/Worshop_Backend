using Application.DTOs.JobCards;
using Application.Pagination;
using Domain.Enums;

namespace Application.Services.Interfaces;

public interface IJobCardService
{
    Task<JobCardResponse> CreateAsync(Guid actorUserId, Guid branchId, JobCardCreateRequest request, CancellationToken ct = default);
    Task<PageResponse<JobCardResponse>> GetPagedAsync(Guid branchId, PageRequest request, CancellationToken ct = default);
    Task<JobCardResponse> GetByIdAsync(Guid branchId, Guid id, CancellationToken ct = default);

    Task<JobCardResponse> CheckInAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
    Task<JobCardResponse> CheckOutAsync(Guid actorUserId, Guid branchId, Guid id, CancellationToken ct = default);
    Task<JobCardResponse> ChangeStatusAsync(Guid actorUserId, Guid branchId, Guid id, JobCardStatus status, CancellationToken ct = default);
    Task<JobCardResponse> UpdateDiagnosisAsync(Guid actorUserId, Guid branchId, Guid id, string? diagnosis, CancellationToken ct = default);
}
