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
        // Basic date validation.
        if (dto.CheckOutDate <= dto.CheckInDate)
            throw new ValidationException("CheckOutDate must be after CheckInDate.");

        // Make sure the trip exists and belongs to this traveler.
        var trip = await _db.Trips.FindAsync(dto.TripId);
        if (trip == null)
            throw new NotFoundException($"Trip with ID {dto.TripId} was not found.");
        if (trip.TravelerId != travelerId)
            throw new ForbiddenException("You can only create bookings for your own trips.");

        // Booking dates must fall within the trip's date range.
        if (dto.CheckInDate < trip.StartDate || dto.CheckOutDate > trip.EndDate)
            throw new ValidationException(
                $"Booking dates must fall within the trip's date range ({trip.StartDate:yyyy-MM-dd} \u2013 {trip.EndDate:yyyy-MM-dd}).");

        // Make sure the room exists and is Active.
        var room = await _db.Rooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == dto.RoomId);
        if (room == null)
            throw new NotFoundException($"Room with ID {dto.RoomId} was not found.");
        if (room.Status != RoomStatus.Active)
            throw new ValidationException("This room type is not currently available for booking.");
        if (room.Hotel.Status != HotelStatus.Active)
            throw new ValidationException("This hotel is not currently accepting bookings.");

        // Check availability using the same shared method as the search endpoint.
        // This prevents double-booking beyond TotalRooms capacity.
        int alreadyBooked = await _hotelService.CountBookedRoomsAsync(
            dto.RoomId, dto.CheckInDate, dto.CheckOutDate);
        int available = room.TotalRooms - alreadyBooked;

        if (dto.NumberOfRooms > available)
            throw new ValidationException(
                $"Not enough rooms available. Requested: {dto.NumberOfRooms}, available: {available}.");

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
}
