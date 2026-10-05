using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class HotelBookingService : IHotelBookingService
{
    private readonly AppDbContext _db;
    private readonly IHotelService _hotelService;
    private readonly ICheckoutService _checkoutService;

    public HotelBookingService(AppDbContext db, IHotelService hotelService, ICheckoutService checkoutService)
    {
        _db              = db;
        _hotelService    = hotelService;
        _checkoutService = checkoutService;
    }

    public async Task<HotelBookingSummaryDto> CreateAsync(CreateHotelBookingDto dto, int travelerId)
    {
        dto.CheckInDate = DateTime.SpecifyKind(dto.CheckInDate, DateTimeKind.Utc);
        dto.CheckOutDate = DateTime.SpecifyKind(dto.CheckOutDate, DateTimeKind.Utc);

        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == dto.TripId);
        if (trip == null)
            throw new NotFoundException($"Trip with ID {dto.TripId} was not found.");

        if (trip.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to modify this trip.");

        if (trip.Status == TripStatus.Confirmed || trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled)
            throw new ValidationException($"Cannot add bookings to a trip that is {trip.Status}.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (_db.Database.IsRelational())
            {
                long roomLockKey = 1_000_000L + dto.RoomId;
                await _db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({roomLockKey})");
            }

            await ValidateAndCheckAvailabilityAsync(
                dto.RoomId, dto.CheckInDate, dto.CheckOutDate, dto.NumberOfRooms, trip, null);

            var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId);
            int nights = (int)(dto.CheckOutDate.Date - dto.CheckInDate.Date).TotalDays;
            decimal snapshot = room!.PricePerNight * nights * dto.NumberOfRooms;

            var booking = new HotelBooking
            {
                TripId = dto.TripId,
                RoomId = dto.RoomId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                NumberOfRooms = dto.NumberOfRooms,
                Status = BookingStatus.Held,
                PriceSnapshot = snapshot,
                HoldExpiresAt = DateTime.UtcNow.AddHours(12),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.HotelBookings.Add(booking);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var saved = await _db.HotelBookings
                .Include(b => b.Room).ThenInclude(r => r.Hotel)
                .FirstAsync(b => b.Id == booking.Id);

            return saved.ToSummaryDto();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<HotelBookingSummaryDto> UpdateAsync(int id, UpdateHotelBookingDto dto, int travelerId)
    {
        // Force UTC for Npgsql timestamp with time zone columns
        dto.CheckInDate = DateTime.SpecifyKind(dto.CheckInDate, DateTimeKind.Utc);
        dto.CheckOutDate = DateTime.SpecifyKind(dto.CheckOutDate, DateTimeKind.Utc);

        var booking = await _db.HotelBookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            throw new NotFoundException($"Hotel booking with ID {id} was not found.");

        if (booking.Trip.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to modify this booking.");

        if (booking.Status != BookingStatus.Held)
            throw new ValidationException($"Cannot modify a booking that is already {booking.Status}.");

        // Guard: do not allow editing an expired hold
        if (booking.HoldExpiresAt.HasValue && booking.HoldExpiresAt < DateTime.UtcNow)
            throw new ValidationException(
                "This hold has expired. Please place a new checkout hold to rebook.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (_db.Database.IsRelational())
            {
                // We use a small hash prefix (1_000_000) for room locks to match CheckoutService
                long roomLockKey = 1_000_000L + dto.RoomId;
                await _db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({roomLockKey})");
            }

            await ValidateAndCheckAvailabilityAsync(
                dto.RoomId, dto.CheckInDate, dto.CheckOutDate, dto.NumberOfRooms, booking.Trip, booking.Id);

            booking.RoomId = dto.RoomId;
            booking.CheckInDate = dto.CheckInDate;
            booking.CheckOutDate = dto.CheckOutDate;
            booking.NumberOfRooms = dto.NumberOfRooms;
            booking.UpdatedAt = DateTime.UtcNow;

            // If it was linked to a checkout, the checkout snapshot price is now invalidated,
            // but we don't recalculate the snapshot here because this is a legacy edit.
            // We just update the DB. TotalPrice will fallback to dynamic calculation.

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        var saved = await _db.HotelBookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .Include(b => b.Checkout)
            .FirstAsync(b => b.Id == booking.Id);

        return saved.ToSummaryDto();
    }

    public async Task DeleteAsync(int id, int requestingUserId, string requestingUserRole)
    {
        var booking = await _db.HotelBookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            throw new NotFoundException($"Hotel booking with ID {id} was not found.");

        bool isOwner = booking.Trip.TravelerId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        if (!isOwner && !isAdmin)
            throw new ForbiddenException("You do not have permission to delete this booking.");

        if (booking.Status != BookingStatus.Held)
            throw new ValidationException($"Cannot modify a booking that is already {booking.Status}.");

        _db.HotelBookings.Remove(booking);
        await _db.SaveChangesAsync();
    }

    // Returns all hotel bookings that belong to the requesting traveler
    // (bookings are linked to trips, which are owned by the traveler).
    public async Task<PagedResult<HotelBookingSummaryDto>> GetMyBookingsAsync(
        int travelerId, string? status, int page, int pageSize)
    {
        var query = _db.HotelBookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .Include(b => b.Trip)
            .Include(b => b.Checkout)
            .Where(b => b.Trip.TravelerId == travelerId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        query = query.OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<HotelBookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<HotelBookingSummaryDto>> GetMyHotelsBookingsAsync(
        int ownerId, string? search, string? status, int page, int pageSize)
    {
        var query = _db.HotelBookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .Where(b => b.Room.Hotel.OwnerId == ownerId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var statusEnum))
            query = query.Where(b => b.Status == statusEnum);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Room.Hotel.Name.Contains(search) || b.Room.RoomType.Contains(search));

        query = query.OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<HotelBookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // Cancel a booking. Only the trip owner or an Admin/SuperAdmin can do this.
    // Cancellation is allowed while the booking is Held or Confirmed.
    public async Task CancelAsync(int bookingId, int requestingUserId, string requestingUserRole)
    {
        var booking = await _db.HotelBookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new NotFoundException($"Hotel booking with ID {bookingId} was not found.");

        bool isOwner = booking.Trip.TravelerId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        if (!isOwner && !isAdmin)
            throw new ForbiddenException("You do not have permission to cancel this booking.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new ValidationException("This booking is already cancelled.");

        if (booking.Status == BookingStatus.Confirmed)
            throw new ValidationException("Confirmed bookings cannot be cancelled online as there is no refund workflow.");

        booking.Status    = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task ValidateAndCheckAvailabilityAsync(
        int roomId, DateTime checkInDate, DateTime checkOutDate, int numberOfRooms, Trip trip, int? excludeBookingId)
    {
        if (checkOutDate <= checkInDate)
            throw new ValidationException("CheckOutDate must be after CheckInDate.");

        if (checkInDate.Date < trip.StartDate.Date || checkOutDate.Date > trip.EndDate.Date)
            throw new ValidationException(
                $"Booking dates must fall within the trip's date range ({trip.StartDate:yyyy-MM-dd} \u2013 {trip.EndDate:yyyy-MM-dd}).");

        var room = await _db.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null)
            throw new NotFoundException($"Room with ID {roomId} was not found.");
        if (room.Status != RoomStatus.Active)
            throw new ValidationException("This room type is not currently available for booking.");
        if (room.Hotel.Status != HotelStatus.Active)
            throw new ValidationException("This hotel is not currently accepting bookings.");

        int alreadyBooked = await _hotelService.CountBookedRoomsAsync(
            roomId, checkInDate, checkOutDate, excludeBookingId);
        int available = room.TotalRooms - alreadyBooked;

        if (numberOfRooms > available)
            throw new ValidationException(
                $"Not enough rooms available. Requested: {numberOfRooms}, available: {available}.");
    }
}
