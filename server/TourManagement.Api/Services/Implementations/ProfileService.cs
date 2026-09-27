using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Profile;
using TourManagement.Api.Dtos.User;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Profile;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Services.Implementations;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserSummaryDto> GetMyProfileAsync(int userId)
    {
        var user = await GetUserOrThrowAsync(userId);
        return user.ToSummaryDto();
    }

    public async Task<UserSummaryDto> UpdateMyProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await GetUserOrThrowAsync(userId);

        // Enforce email uniqueness (ignoring this user's current email)
        bool emailExists = await _db.Users
            .AnyAsync(u => u.Email == dto.Email && u.Id != userId);

        if (emailExists)
            throw new ValidationException("This email address is already registered to another account.");

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return user.ToSummaryDto();
    }

    public async Task<DeletionEligibilityDto> CheckDeletionEligibilityAsync(int userId, string role)
    {
        IAccountDeletionGuard? guard = role switch
        {
            Roles.HotelOwner => new HotelOwnerDeletionGuard(_db),
            Roles.TransportProvider => new TransportProviderDeletionGuard(_db),
            Roles.Supplier => new SupplierDeletionGuard(_db),
            _ => null // Traveler, Admin, SuperAdmin have no blocking rules yet
        };

        if (guard == null)
        {
            return new DeletionEligibilityDto { CanDelete = true };
        }

        return await guard.CheckAsync(userId);
    }

    public async Task DeleteMyAccountAsync(int userId, string role)
    {
        var user = await GetUserOrThrowAsync(userId);

        var eligibility = await CheckDeletionEligibilityAsync(userId, role);
        if (!eligibility.CanDelete)
        {
            throw new ValidationException(eligibility.BlockingMessage ?? "You cannot delete your account at this time.");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    private async Task<User> GetUserOrThrowAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} was not found.");
        return user;
    }
}
