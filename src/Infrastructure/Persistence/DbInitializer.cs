using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task EnsureExtensionsAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Needed for gen_random_uuid()
        await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pgcrypto;", ct);
    }

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.Branches.AnyAsync(ct))
        {
            db.Branches.AddRange(
                new Branch { Name = "Branch 1", Address = "Main Road", IsActive = true },
                new Branch { Name = "Branch 2", Address = "2nd Street", IsActive = true }
            );
            await db.SaveChangesAsync(ct);
        }

        var b1 = await db.Branches.OrderBy(x => x.CreatedAt).FirstAsync(ct);

        if (!await db.Users.AnyAsync(ct))
        {
            db.Users.AddRange(
                new User { Email = "admin@demo.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), Role = UserRole.HQ_ADMIN, BranchId = null, IsActive = true },
                new User { Email = "manager@branch1.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"), Role = UserRole.BRANCH_MANAGER, BranchId = b1.Id, IsActive = true },
                new User { Email = "store@branch1.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Store@123"), Role = UserRole.STOREKEEPER, BranchId = b1.Id, IsActive = true },
                new User { Email = "cashier@branch1.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cashier@123"), Role = UserRole.CASHIER, BranchId = b1.Id, IsActive = true },
                new User { Email = "tech@branch1.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tech@123"), Role = UserRole.TECHNICIAN, BranchId = b1.Id, IsActive = true }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Locations.AnyAsync(ct))
        {
            db.Locations.AddRange(
                new Location { BranchId = b1.Id, Code = "STORE", Name = "Store" },
                new Location { BranchId = b1.Id, Code = "WORKSHOP", Name = "Workshop" }
            );
            await db.SaveChangesAsync(ct);
        }

        if (!await db.WorkStations.AnyAsync(ct))
        {
            db.WorkStations.AddRange(
                new WorkStation { BranchId = b1.Id, Code = "RECEPTION", Name = "Reception" },
                new WorkStation { BranchId = b1.Id, Code = "LIFT_1", Name = "Lift Bay 1" },
                new WorkStation { BranchId = b1.Id, Code = "ELECTRICAL", Name = "Electrical" }
            );
            await db.SaveChangesAsync(ct);
        }
    }
}
