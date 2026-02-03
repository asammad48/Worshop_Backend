using Application.DTOs.Customers; using Application.Pagination;
namespace Application.Services.Interfaces;
public interface ICustomerService{Task<CustomerResponse>CreateAsync(CustomerCreateRequest r,CancellationToken ct=default);Task<PageResponse<CustomerResponse>>GetPagedAsync(PageRequest r,CancellationToken ct=default);Task<CustomerResponse>GetByIdAsync(Guid id,CancellationToken ct=default);} 
