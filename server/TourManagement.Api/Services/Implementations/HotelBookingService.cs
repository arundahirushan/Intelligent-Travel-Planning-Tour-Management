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
    private readonly IHotelService _hotelService;  // reuses CountBookedRoomsAsync

    public HotelBookingService(AppDbContext db, IHotelService hotelService)
    {
        _db           = db;
        _hotelService = hotelService;
    }

    // Create a new hotel booking (starts as Held).
    // Validates dates, checks that the trip belongs to this traveler, and
    // ensures the room has enough availability for the requested dates.
    public async Task<HotelBookingSummaryDto> CreateAsync(CreateHotelBookingDto dto, int travelerId)
    {
        var trip = await _db.Trips.FindAsync(dto.TripId);
        if (trip == null)
            throw new NotFoundException($"Trip with ID {dto.TripId} was not found.");
        if (trip.TravelerId != travelerId)
            throw new ForbiddenException("You can only create bookings for your own trips.");

        await ValidateAndCheckAvailabilityAsync(
            dto.RoomId, dto.CheckInDate, dto.CheckOutDate, dto.NumberOfRooms, trip, null);

        var booking = new HotelBooking
        {
            TripId        = dto.TripId,
            RoomId        = dto.RoomId,
            CheckInDate   = dto.CheckInDate,
            CheckOutDate  = dto.CheckOutDate,
            NumberOfRooms = dto.NumberOfRooms,
            Status        = BookingStatus.Held,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };

        _db.HotelBookings.Add(booking);
        await _db.SaveChangesAsync();

        // Reload with Room.Hotel so ToSummaryDto can compute TotalPrice.
        var saved = await _db.HotelBookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .FirstAsync(b => b.Id == booking.Id);

        return saved.ToSummaryDto();
    }

    public async Task<HotelBookingSummaryDto> UpdateAsync(int id, UpdateHotelBookingDto dto, int travelerId)
    {
        var booking = await _db.HotelBookings
            .Include(b => b.Trip)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            throw new NotFoundException($"Hotel booking with ID {id} was not found.");

        if (booking.Trip.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to modify this booking.");

        if (booking.Status != BookingStatus.Held)
            throw new ValidationException($"Cannot modify a booking that is already {booking.Status}.");

        await ValidateAndCheckAvailabilityAsync(
            dto.RoomId, dto.CheckInDate, dto.CheckOutDate, dto.NumberOfRooms, booking.Trip, booking.Id);

        booking.RoomId = dto.RoomId;
        booking.CheckInDate = dto.CheckInDate;
        booking.CheckOutDate = dto.CheckOutDate;
        booking.NumberOfRooms = dto.NumberOfRooms;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var saved = await _db.HotelBookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
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

        booking.Status    = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task ValidateAndCheckAvailabilityAsync(
        int roomId, DateTime checkInDate, DateTime checkOutDate, int numberOfRooms, Trip trip, int? excludeBookingId)
    {
        if (checkOutDate <= checkInDate)
            throw new ValidationException("CheckOutDate must be after CheckInDate.");

        if (checkInDate < trip.StartDate || checkOutDate > trip.EndDate)
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
