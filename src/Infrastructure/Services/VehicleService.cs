using Application.DTOs.Vehicles;
using Application.Pagination;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Errors;

namespace Infrastructure.Services;

public sealed class VehicleService : IVehicleService
{
    private readonly AppDbContext _db;
    public VehicleService(AppDbContext db) { _db = db; }

    public async Task<VehicleResponse> CreateAsync(VehicleCreateRequest r, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var plate = (r.Plate ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(plate)) errors.Add("Plate is required.");
        if (r.CustomerId == Guid.Empty) errors.Add("CustomerId is required.");
        if (errors.Count > 0) throw new ValidationException("Validation failed", errors);

        var customerExists = await _db.Customers.AnyAsync(x => x.Id == r.CustomerId && !x.IsDeleted, ct);
        if (!customerExists) throw new NotFoundException("Customer not found");

        var plateExists = await _db.Vehicles.AnyAsync(x => x.Plate == plate && !x.IsDeleted, ct);
        if (plateExists) throw new DomainException("Plate already exists", 409, new[] { "Vehicle plate must be unique." });

        var entity = new Domain.Entities.Vehicle
        {
            Plate = plate,
            Make = r.Make?.Trim(),
            Model = r.Model?.Trim(),
            Year = r.Year,
            CustomerId = r.CustomerId
        };
        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new VehicleResponse(entity.Id, entity.Plate, entity.Make, entity.Model, entity.Year, entity.CustomerId);
    }

    public async Task<PageResponse<VehicleResponse>> GetPagedAsync(PageRequest r, CancellationToken ct = default)
    {
        var q = _db.Vehicles.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToUpperInvariant();
            q = q.Where(x => x.Plate.Contains(s));
        }
        var total = await q.CountAsync(ct);
        var page = Math.Max(1, r.PageNumber);
        var size = Math.Clamp(r.PageSize, 1, 100);
        var items = await q.OrderBy(x => x.Plate)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new VehicleResponse(x.Id, x.Plate, x.Make, x.Model, x.Year, x.CustomerId))
            .ToListAsync(ct);
        return new PageResponse<VehicleResponse>(items, total, page, size);
    }

    public async Task<VehicleResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (v is null) throw new NotFoundException("Vehicle not found");
        return new VehicleResponse(v.Id, v.Plate, v.Make, v.Model, v.Year, v.CustomerId);
    }
}
