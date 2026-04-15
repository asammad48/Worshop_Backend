using Application.DTOs.Drivers;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class DriverService : IDriverService
{
    private readonly AppDbContext _db;

    public DriverService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DriverResponse> CreateAsync(DriverCreateRequest request, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (request.CustomerId == Guid.Empty) errors.Add("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(request.FullName)) errors.Add("FullName is required.");
        if (errors.Count > 0) throw new ValidationException("Validation failed", errors);

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CustomerId && !x.IsDeleted, ct);
        if (customer is null) throw new NotFoundException("Customer not found");

        var entity = new Domain.Entities.Driver
        {
            CustomerId = request.CustomerId,
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            LicenseNumber = request.LicenseNumber?.Trim()
        };

        _db.Drivers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new DriverResponse(
            entity.Id,
            entity.CustomerId,
            customer.FullName,
            entity.FullName,
            entity.Phone,
            entity.LicenseNumber);
    }

    public async Task<PageResponse<DriverResponse>> GetPagedAsync(DriverQueryRequest request, CancellationToken ct = default)
    {
        var q = _db.Drivers
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => !x.IsDeleted && x.Customer != null && !x.Customer.IsDeleted);

        if (request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
            q = q.Where(x => x.CustomerId == request.CustomerId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            q = q.Where(x => x.FullName.ToLower().Contains(s)
                || (x.Phone != null && x.Phone.ToLower().Contains(s))
                || (x.LicenseNumber != null && x.LicenseNumber.ToLower().Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 1, 100);

        var items = await q
            .OrderBy(x => x.FullName)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new DriverResponse(
                x.Id,
                x.CustomerId,
                x.Customer != null ? x.Customer.FullName : string.Empty,
                x.FullName,
                x.Phone,
                x.LicenseNumber))
            .ToListAsync(ct);

        return new PageResponse<DriverResponse>(items, total, page, size);
    }

    public async Task<DriverResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var driver = await _db.Drivers
            .AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (driver is null || driver.Customer is null || driver.Customer.IsDeleted)
            throw new NotFoundException("Driver not found");

        return new DriverResponse(
            driver.Id,
            driver.CustomerId,
            driver.Customer.FullName,
            driver.FullName,
            driver.Phone,
            driver.LicenseNumber);
    }
}
