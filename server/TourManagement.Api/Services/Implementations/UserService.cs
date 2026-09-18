using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.User;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    // List users waiting for an admin to approve their account.
    public async Task<PagedResult<UserSummaryDto>> GetPendingAsync(string? role, int page, int pageSize)
    {
        var query = _db.Users
            .Where(u => u.Status == UserStatus.PendingApproval);

        // Optionally filter by role (e.g. only show pending HotelOwners).
        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<UserSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // Approve a pending account — sets Status to Active so they can log in.
    public async Task<UserSummaryDto> ApproveAsync(int id)
    {
        var user = await GetUserOrThrowAsync(id);

        if (user.Status != UserStatus.PendingApproval)
            throw new ValidationException("Only accounts with PendingApproval status can be approved.");

        user.Status    = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return user.ToSummaryDto();
    }

    // Reject a pending account.
    public async Task<UserSummaryDto> RejectAsync(int id)
    {
        var user = await GetUserOrThrowAsync(id);

        if (user.Status != UserStatus.PendingApproval)
            throw new ValidationException("Only accounts with PendingApproval status can be rejected.");

        user.Status    = UserStatus.Rejected;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return user.ToSummaryDto();
    }

    // General admin overview — all users with optional search, filter, sort.
    public async Task<PagedResult<UserSummaryDto>> GetAllAsync(
        string? search, string? role, string? status, string? sort, int page, int pageSize)
    {
        var query = _db.Users.AsQueryable();

        // Search by name or email (case-insensitive via EF Core's translation).
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        // Parse the status string to the enum if provided.
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
            query = query.Where(u => u.Status == statusEnum);

        // Simple sort options — default is newest first.
        query = sort switch
        {
            "name"   => query.OrderBy(u => u.FullName),
            "email"  => query.OrderBy(u => u.Email),
            "oldest" => query.OrderBy(u => u.CreatedAt),
            _        => query.OrderByDescending(u => u.CreatedAt)  // default: newest first
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<UserSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // SuperAdmin only: create an Admin account directly (bypasses registration).
    public async Task<UserSummaryDto> CreateAdminAsync(CreateAdminRequestDto dto)
    {
        bool emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
            throw new ValidationException("This email address is already registered.");

        var admin = new User
        {
            FullName     = dto.FullName,
            Email        = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            // Role is always Admin here — never taken from client input.
            Role         = Roles.Admin,
            Status       = UserStatus.Active,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        return admin.ToSummaryDto();
    }

    // SuperAdmin only: elevate an Admin to SuperAdmin.
    public async Task<UserSummaryDto> PromoteToSuperAdminAsync(int id)
    {
        var user = await GetUserOrThrowAsync(id);

        // Only makes sense to promote someone who is currently an Admin.
        if (user.Role != Roles.Admin)
            throw new ValidationException("Only Admin users can be promoted to SuperAdmin.");

        user.Role      = Roles.SuperAdmin;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return user.ToSummaryDto();
    }

    // SuperAdmin only: permanently delete a user account.
    // Their trips (and itinerary items) are cascade-deleted by the database.
    public async Task DeleteUserAsync(int id)
    {
        var user = await GetUserOrThrowAsync(id);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    // Helper: fetch a user by ID or throw a clear NotFoundException.
    private async Task<User> GetUserOrThrowAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} was not found.");
        return user;
    }
}
