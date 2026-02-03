using Application.DTOs.Branches; using Application.Pagination;
namespace Application.Services.Interfaces;
public interface IBranchService{Task<BranchResponse>CreateAsync(BranchCreateRequest r,CancellationToken ct=default);Task<PageResponse<BranchResponse>>GetPagedAsync(PageRequest r,CancellationToken ct=default);Task<BranchResponse>GetByIdAsync(Guid id,CancellationToken ct=default);} 
