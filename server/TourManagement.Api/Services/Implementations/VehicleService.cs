using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class VehicleService : IVehicleService
{
    private readonly AppDbContext _db;

    public VehicleService(AppDbContext db)
    {
        _db = db;
    }

    // ── TransportProvider: create / manage own vehicles ──────────────────────

    public async Task<VehicleDetailDto> CreateVehicleAsync(CreateVehicleDto dto, int providerId)
    {
        // Registration numbers must be unique across all vehicles.
        bool regExists = await _db.Vehicles.AnyAsync(v => v.RegistrationNumber == dto.RegistrationNumber);
        if (regExists)
            throw new ValidationException($"A vehicle with registration number '{dto.RegistrationNumber}' already exists.");

        var vehicle = dto.ToEntity(providerId);
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        return await LoadVehicleDetailAsync(vehicle.Id);
    }

    public async Task<PagedResult<VehicleSummaryDto>> GetMyVehiclesAsync(
        int providerId, string? search, string? status, string? sort, int page, int pageSize)
    {
        var query = _db.Vehicles
            .Include(v => v.Provider)
            .Where(v => v.ProviderId == providerId);

        // Search by vehicle type or model (free-text contains).
        if (!string.IsNullOrEmpty(search))
            query = query.Where(v => v.VehicleType.Contains(search) || v.Model.Contains(search));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<VehicleStatus>(status, out var statusEnum))
            query = query.Where(v => v.Status == statusEnum);

        query = sort switch
        {
            "price"   => query.OrderBy(v => v.PricePerDay),
            "status"  => query.OrderBy(v => v.Status),
            "oldest"  => query.OrderBy(v => v.CreatedAt),
            _         => query.OrderByDescending(v => v.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<VehicleDetailDto> GetMyVehicleByIdAsync(int vehicleId, int providerId)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);
        CheckOwner(vehicle, providerId);
        return await LoadVehicleDetailAsync(vehicleId);
    }

    public async Task<VehicleDetailDto> UpdateVehicleAsync(int vehicleId, UpdateVehicleDto dto, int requestingUserId)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);
        CheckOwner(vehicle, requestingUserId);

        // If registration number is changing, make sure the new one isn't already taken.
        if (vehicle.RegistrationNumber != dto.RegistrationNumber)
        {
            bool regExists = await _db.Vehicles.AnyAsync(
                v => v.RegistrationNumber == dto.RegistrationNumber && v.Id != vehicleId);
            if (regExists)
                throw new ValidationException($"A vehicle with registration number '{dto.RegistrationNumber}' already exists.");
        }

        vehicle.UpdateFromDto(dto);
        await _db.SaveChangesAsync();
        return await LoadVehicleDetailAsync(vehicleId);
    }

    // Soft-delete: sets Status = Inactive so booking history is not lost.
    public async Task DeactivateVehicleAsync(int vehicleId, int requestingUserId)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);
        CheckOwner(vehicle, requestingUserId);

        vehicle.Status    = VehicleStatus.Inactive;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Provider sees all bookings for one of their vehicles (read-only, for managing schedule).
    public async Task<PagedResult<VehicleBookingSummaryDto>> GetVehicleBookingsAsync(
        int vehicleId, int requestingUserId, int page, int pageSize)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);
        CheckOwner(vehicle, requestingUserId);

        var query = _db.VehicleBookings
            .Include(b => b.Vehicle)
            .Where(b => b.VehicleId == vehicleId)
            .OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleBookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    public async Task<PagedResult<VehicleSummaryDto>> GetAllVehiclesAsync(
        string? status, int? providerId, string? search, string? sort, int page, int pageSize)
    {
        var query = _db.Vehicles
            .Include(v => v.Provider)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<VehicleStatus>(status, out var statusEnum))
            query = query.Where(v => v.Status == statusEnum);

        if (providerId.HasValue)
            query = query.Where(v => v.ProviderId == providerId.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(v => v.VehicleType.Contains(search) || v.Model.Contains(search));

        query = sort switch
        {
            "price"   => query.OrderBy(v => v.PricePerDay),
            "status"  => query.OrderBy(v => v.Status),
            "oldest"  => query.OrderBy(v => v.CreatedAt),
            _         => query.OrderByDescending(v => v.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<VehicleSummaryDto>> GetPendingVehiclesAsync(int page, int pageSize)
    {
        var query = _db.Vehicles
            .Include(v => v.Provider)
            .Where(v => v.Status == VehicleStatus.PendingApproval)
            .OrderBy(v => v.CreatedAt);  // oldest first so nothing waits forever

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => v.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task ApproveVehicleAsync(int vehicleId)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);

        if (vehicle.Status != VehicleStatus.PendingApproval)
            throw new ValidationException("Only vehicles with PendingApproval status can be approved.");

        vehicle.Status    = VehicleStatus.Active;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Reject a pending listing — only valid while the vehicle is still PendingApproval.
    // Use SuspendVehicleAsync instead for vehicles that are already Active.
    public async Task RejectVehicleAsync(int vehicleId)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);

        if (vehicle.Status != VehicleStatus.PendingApproval)
            throw new ValidationException(
                "Only PendingApproval vehicles can be rejected. " +
                "To disable an Active vehicle, use the suspend endpoint instead.");

        vehicle.Status    = VehicleStatus.Rejected;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Suspend an already-Active listing (policy violation, complaints, etc.).
    // For rejecting a pending listing, use RejectVehicleAsync instead.
    public async Task SuspendVehicleAsync(int vehicleId)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);

        if (vehicle.Status != VehicleStatus.Active)
            throw new ValidationException(
                "Only Active vehicles can be suspended. " +
                "Use the reject endpoint for PendingApproval vehicles.");

        vehicle.Status    = VehicleStatus.Suspended;
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Public / Traveler: search and view ───────────────────────────────────

    // Main availability search. Each vehicle is one physical unit — it's either
    // available for the whole window or it isn't (simpler than room-type math).
    public async Task<List<VehicleSearchResultDto>> SearchAsync(VehicleSearchRequestDto request)
    {
        if (request.EndDate <= request.StartDate)
            throw new ValidationException("EndDate must be after StartDate.");

        // Step 1: Get all active vehicles.
        var vehicles = await _db.Vehicles
            .Where(v => v.Status == VehicleStatus.Active)
            .ToListAsync();

        var results = new List<VehicleSearchResultDto>();

        foreach (var vehicle in vehicles)
        {
            // Step 2: Skip vehicles that are already booked for an overlapping date range.
            bool available = await IsVehicleAvailableAsync(vehicle.Id, request.StartDate, request.EndDate);
            if (!available) continue;

            // Step 3: Apply optional filters.
            if (request.MinCapacity.HasValue && vehicle.Capacity < request.MinCapacity.Value)
                continue;

            if (request.MaxPricePerDay.HasValue && vehicle.PricePerDay > request.MaxPricePerDay.Value)
                continue;

            results.Add(new VehicleSearchResultDto
            {
                VehicleId   = vehicle.Id,
                VehicleType = vehicle.VehicleType,
                Model       = vehicle.Model,
                Capacity    = vehicle.Capacity,
                PricePerDay = vehicle.PricePerDay
            });
        }

        // Step 4: Sort by price ascending.
        results = results.OrderBy(r => r.PricePerDay).ToList();

        return results;
    }

    // Public vehicle detail — only Active vehicles are visible to travelers.
    // The owner and admins can still see any status.
    public async Task<VehicleDetailDto> GetPublicVehicleByIdAsync(
        int vehicleId, int requestingUserId, string requestingUserRole)
    {
        var vehicle = await GetVehicleOrThrowAsync(vehicleId);

        bool isOwner = vehicle.ProviderId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        // Non-owners and non-admins can only see Active vehicles.
        if (!isOwner && !isAdmin && vehicle.Status != VehicleStatus.Active)
            throw new NotFoundException($"Vehicle with ID {vehicleId} was not found.");

        return await LoadVehicleDetailAsync(vehicleId);
    }

    // ── Shared helper: overlap availability check ────────────────────────────

    // Returns true if the vehicle has no Held or Confirmed booking whose date range
    // overlaps with [startDate, endDate).
    //
    // This same logic is reused by SearchAsync (above) and VehicleBookingService.CreateAsync
    // (to validate that a new booking won't double-book the vehicle).
    //
    // The overlap formula is: startA < endB AND startB < endA.
    // See Common/DateRangeHelper.cs for the canonical definition used in non-EF code.
    // EF Core LINQ cannot call DateRangeHelper.HasOverlap directly, so the two
    // conditions are kept inline below so EF can translate them to SQL.
    public async Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime startDate, DateTime endDate)
    {
        bool hasOverlap = await _db.VehicleBookings.AnyAsync(b =>
            b.VehicleId == vehicleId
            && (b.Status == BookingStatus.Held || b.Status == BookingStatus.Confirmed)
            && b.StartDate < endDate    // overlap condition part 1
            && b.EndDate   > startDate  // overlap condition part 2
        );

        return !hasOverlap;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Loads a vehicle with Provider fully populated for the detail DTO.
    private async Task<VehicleDetailDto> LoadVehicleDetailAsync(int vehicleId)
    {
        var vehicle = await _db.Vehicles
            .Include(v => v.Provider)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (vehicle == null)
            throw new NotFoundException($"Vehicle with ID {vehicleId} was not found.");

        return vehicle.ToDetailDto();
    }

    // Fetches a Vehicle by ID or throws NotFoundException.
    private async Task<Vehicle> GetVehicleOrThrowAsync(int vehicleId)
    {
        var vehicle = await _db.Vehicles.FindAsync(vehicleId);
        if (vehicle == null)
            throw new NotFoundException($"Vehicle with ID {vehicleId} was not found.");
        return vehicle;
    }

    // Throws ForbiddenException if the requesting user doesn't own this vehicle.
    private static void CheckOwner(Vehicle vehicle, int requestingUserId)
    {
        if (vehicle.ProviderId != requestingUserId)
            throw new ForbiddenException("You do not have permission to manage this vehicle.");
    }
}

