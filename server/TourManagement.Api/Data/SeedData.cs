using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Models;

namespace TourManagement.Api.Data;

// Seeds required startup data into the database.
// Called from Program.cs once on every startup — safe to call multiple times
// because we check before inserting.
public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        // Get a scoped DbContext from the DI container.
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Only seed if there is no SuperAdmin yet — prevents duplicates on restart.
        bool superAdminExists = await db.Users.AnyAsync(u => u.Role == Roles.SuperAdmin);
        if (superAdminExists) return;

        // TODO: Change this email and password before any real deployment.
        // This is a placeholder that exists purely to bootstrap the system
        // so that someone can log in and create real Admin accounts.
        var superAdmin = new User
        {
            FullName     = "Super Admin",
            Email        = "superadmin@tourmanagement.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123"),
            Role         = Roles.SuperAdmin,
            Status       = UserStatus.Active,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();
    }
}
