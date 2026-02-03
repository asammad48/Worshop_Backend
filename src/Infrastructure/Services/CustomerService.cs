using Application.DTOs.Customers;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    public CustomerService(AppDbContext db) { _db = db; }

    public async Task<CustomerResponse> CreateAsync(CustomerCreateRequest r, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(r.FullName)) errors.Add("FullName is required.");
        if (errors.Count > 0) throw new ValidationException("Validation failed", errors);

        var entity = new Domain.Entities.Customer
        {
            FullName = r.FullName.Trim(),
            Phone = r.Phone?.Trim(),
            Email = r.Email?.Trim(),
            NationalId = r.NationalId?.Trim()
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new CustomerResponse(entity.Id, entity.FullName, entity.Phone, entity.Email, entity.NationalId);
    }

    public async Task<PageResponse<CustomerResponse>> GetPagedAsync(PageRequest r, CancellationToken ct = default)
    {
        var q = _db.Customers.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToLower();
            q = q.Where(x => x.FullName.ToLower().Contains(s) || (x.Phone != null && x.Phone.ToLower().Contains(s)));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.FullName)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new CustomerResponse(x.Id, x.FullName, x.Phone, x.Email, x.NationalId))
            .ToListAsync(ct);
        return new PageResponse<CustomerResponse>(items, total, page, size);
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (c is null) throw new NotFoundException("Customer not found");
        return new CustomerResponse(c.Id, c.FullName, c.Phone, c.Email, c.NationalId);
    }
}
