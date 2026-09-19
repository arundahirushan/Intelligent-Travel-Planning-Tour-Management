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

public class VehicleBookingService : IVehicleBookingService
{
    private readonly AppDbContext _db;
    private readonly IVehicleService _vehicleService;  // reuses IsVehicleAvailableAsync

    public VehicleBookingService(AppDbContext db, IVehicleService vehicleService)
    {
        _db             = db;
        _vehicleService = vehicleService;
    }

    // Create a new vehicle booking (starts as Held).
    // Validates dates, checks that the trip belongs to this traveler, and
    // ensures the vehicle is available for the requested dates.
    public async Task<VehicleBookingSummaryDto> CreateAsync(CreateVehicleBookingDto dto, int travelerId)
    {
        // Basic date validation.
        if (dto.EndDate <= dto.StartDate)
            throw new ValidationException("EndDate must be after StartDate.");

        // Make sure the trip exists and belongs to this traveler.
        var trip = await _db.Trips.FindAsync(dto.TripId);
        if (trip == null)
            throw new NotFoundException($"Trip with ID {dto.TripId} was not found.");
        if (trip.TravelerId != travelerId)
            throw new ForbiddenException("You can only create bookings for your own trips.");

        // Booking dates must fall within the trip's date range.
        if (dto.StartDate < trip.StartDate || dto.EndDate > trip.EndDate)
            throw new ValidationException(
                $"Booking dates must fall within the trip's date range ({trip.StartDate:yyyy-MM-dd} \u2013 {trip.EndDate:yyyy-MM-dd}).");

        // Make sure the vehicle exists and is Active.
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null)
            throw new NotFoundException($"Vehicle with ID {dto.VehicleId} was not found.");
        if (vehicle.Status != VehicleStatus.Active)
            throw new ValidationException("This vehicle is not currently available for booking.");

        // Check availability using the same shared method as the search endpoint.
        // This prevents double-booking the same physical vehicle.
        bool available = await _vehicleService.IsVehicleAvailableAsync(dto.VehicleId, dto.StartDate, dto.EndDate);
        if (!available)
            throw new ValidationException(
                "This vehicle is already booked for an overlapping date range. Please choose different dates.");

        var booking = new VehicleBooking
        {
            TripId          = dto.TripId,
            VehicleId       = dto.VehicleId,
            StartDate       = dto.StartDate,
            EndDate         = dto.EndDate,
            PickupLatitude  = dto.PickupLatitude,
            PickupLongitude = dto.PickupLongitude,
            PickupNote      = dto.PickupNote,
            Status          = BookingStatus.Held,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };

        _db.VehicleBookings.Add(booking);
        await _db.SaveChangesAsync();

        // Reload with Vehicle so ToSummaryDto can compute TotalPrice.
        var saved = await _db.VehicleBookings
            .Include(b => b.Vehicle)
            .FirstAsync(b => b.Id == booking.Id);

        return saved.ToSummaryDto();
    }

    // Returns all vehicle bookings that belong to the requesting traveler
    // (bookings are linked to trips, which are owned by the traveler).
    public async Task<PagedResult<VehicleBookingSummaryDto>> GetMyBookingsAsync(
        int travelerId, int page, int pageSize)
    {
        var query = _db.VehicleBookings
            .Include(b => b.Vehicle)
            .Include(b => b.Trip)
            .Where(b => b.Trip.TravelerId == travelerId)
            .OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleBookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // Cancel a booking. Only the trip owner or an Admin/SuperAdmin can do this.
    public async Task CancelAsync(int bookingId, int requestingUserId, string requestingUserRole)
    {
        var booking = await _db.VehicleBookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new NotFoundException($"Vehicle booking with ID {bookingId} was not found.");

        bool isOwner = booking.Trip.TravelerId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        if (!isOwner && !isAdmin)
            throw new ForbiddenException("You do not have permission to cancel this booking.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new ValidationException("This booking is already cancelled.");

        booking.Status    = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Admin oversight view of all vehicle bookings.
    public async Task<PagedResult<VehicleBookingSummaryDto>> GetAllBookingsAsync(
        string? status, int page, int pageSize)
    {
        var query = _db.VehicleBookings
            .Include(b => b.Vehicle)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        query = query.OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<VehicleBookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}

