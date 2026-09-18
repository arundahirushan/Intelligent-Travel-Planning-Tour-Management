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

public class HotelService : IHotelService
{
    private readonly AppDbContext _db;

    public HotelService(AppDbContext db)
    {
        _db = db;
    }

    // ── HotelOwner: create / manage own hotels ───────────────────────────────

    public async Task<HotelDetailDto> CreateHotelAsync(CreateHotelDto dto, int ownerId)
    {
        // Make sure the destination actually exists.
        bool destExists = await _db.Destinations.AnyAsync(d => d.Id == dto.DestinationId);
        if (!destExists)
            throw new NotFoundException($"Destination with ID {dto.DestinationId} was not found.");

        var hotel = dto.ToEntity(ownerId);
        _db.Hotels.Add(hotel);
        await _db.SaveChangesAsync();

        return await LoadHotelDetailAsync(hotel.Id);
    }

    public async Task<PagedResult<HotelSummaryDto>> GetMyHotelsAsync(
        int ownerId, string? search, string? sort, int page, int pageSize)
    {
        var query = _db.Hotels
            .Include(h => h.Destination)
            .Include(h => h.Owner)
            .Where(h => h.OwnerId == ownerId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(h => h.Name.Contains(search));

        query = sort switch
        {
            "status"  => query.OrderBy(h => h.Status),
            "oldest"  => query.OrderBy(h => h.CreatedAt),
            _         => query.OrderByDescending(h => h.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => h.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<HotelSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<HotelDetailDto> GetMyHotelByIdAsync(int hotelId, int ownerId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, ownerId);
        return await LoadHotelDetailAsync(hotelId);
    }

    public async Task<HotelDetailDto> UpdateHotelAsync(int hotelId, UpdateHotelDto dto, int requestingUserId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, requestingUserId);

        // Make sure the new destination exists.
        bool destExists = await _db.Destinations.AnyAsync(d => d.Id == dto.DestinationId);
        if (!destExists)
            throw new NotFoundException($"Destination with ID {dto.DestinationId} was not found.");

        hotel.UpdateFromDto(dto);
        await _db.SaveChangesAsync();
        return await LoadHotelDetailAsync(hotelId);
    }

    // Soft-delete: sets Status = Inactive so booking history is not lost.
    public async Task DeactivateHotelAsync(int hotelId, int requestingUserId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, requestingUserId);

        hotel.Status    = HotelStatus.Inactive;
        hotel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── HotelOwner: manage rooms ─────────────────────────────────────────────

    public async Task<HotelDetailDto> AddRoomAsync(int hotelId, CreateRoomDto dto, int requestingUserId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, requestingUserId);

        var room = dto.ToEntity(hotelId);
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return await LoadHotelDetailAsync(hotelId);
    }

    public async Task<HotelDetailDto> UpdateRoomAsync(int hotelId, int roomId, UpdateRoomDto dto, int requestingUserId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, requestingUserId);

        var room = await GetRoomOrThrowAsync(roomId, hotelId);
        room.UpdateFromDto(dto);
        await _db.SaveChangesAsync();
        return await LoadHotelDetailAsync(hotelId);
    }

    // Soft-delete room: sets Status = Inactive. Won't delete existing booking history.
    public async Task<HotelDetailDto> DeactivateRoomAsync(int hotelId, int roomId, int requestingUserId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, requestingUserId);

        var room = await GetRoomOrThrowAsync(roomId, hotelId);
        room.Status    = RoomStatus.Inactive;
        room.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await LoadHotelDetailAsync(hotelId);
    }

    // HotelOwner sees bookings for their hotel (read-only, for managing availability).
    public async Task<PagedResult<BookingSummaryDto>> GetHotelBookingsAsync(
        int hotelId, int requestingUserId, int page, int pageSize)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);
        CheckOwner(hotel, requestingUserId);

        var query = _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .Where(b => b.Room.HotelId == hotelId)
            .OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => b.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<BookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // ── Admin / SuperAdmin operations ────────────────────────────────────────

    public async Task<PagedResult<HotelSummaryDto>> GetAllHotelsAsync(
        string? status, int? destinationId, int? ownerId,
        string? search, string? sort, int page, int pageSize)
    {
        var query = _db.Hotels
            .Include(h => h.Destination)
            .Include(h => h.Owner)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<HotelStatus>(status, out var statusEnum))
            query = query.Where(h => h.Status == statusEnum);

        if (destinationId.HasValue)
            query = query.Where(h => h.DestinationId == destinationId.Value);

        if (ownerId.HasValue)
            query = query.Where(h => h.OwnerId == ownerId.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(h => h.Name.Contains(search));

        query = sort switch
        {
            "status"  => query.OrderBy(h => h.Status),
            "name"    => query.OrderBy(h => h.Name),
            "oldest"  => query.OrderBy(h => h.CreatedAt),
            _         => query.OrderByDescending(h => h.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => h.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<HotelSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResult<HotelSummaryDto>> GetPendingHotelsAsync(int page, int pageSize)
    {
        var query = _db.Hotels
            .Include(h => h.Destination)
            .Include(h => h.Owner)
            .Where(h => h.Status == HotelStatus.PendingApproval)
            .OrderBy(h => h.CreatedAt);  // oldest first so nothing waits forever

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => h.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<HotelSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task ApproveHotelAsync(int hotelId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);

        if (hotel.Status != HotelStatus.PendingApproval)
            throw new ValidationException("Only hotels with PendingApproval status can be approved.");

        hotel.Status    = HotelStatus.Active;
        hotel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Reject a pending listing — only valid while the hotel is still PendingApproval.
    // Use SuspendHotelAsync instead for hotels that are already Active.
    public async Task RejectHotelAsync(int hotelId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);

        if (hotel.Status != HotelStatus.PendingApproval)
            throw new ValidationException(
                "Only PendingApproval hotels can be rejected. " +
                "To disable an Active hotel, use the suspend endpoint instead.");

        hotel.Status    = HotelStatus.Rejected;
        hotel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Suspend an already-Active listing (policy violation, complaints, etc.).
    // For rejecting a pending listing, use RejectHotelAsync instead.
    public async Task SuspendHotelAsync(int hotelId)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);

        if (hotel.Status != HotelStatus.Active)
            throw new ValidationException(
                "Only Active hotels can be suspended. " +
                "Use the reject endpoint for PendingApproval hotels.");

        hotel.Status    = HotelStatus.Suspended;
        hotel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Admin oversight view of all bookings across all hotels.
    public async Task<PagedResult<BookingSummaryDto>> GetAllBookingsAsync(
        string? status, int page, int pageSize)
    {
        var query = _db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
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

        return new PagedResult<BookingSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // ── Public / Traveler: search and view ───────────────────────────────────

    // Main availability search. Returns ranked list of available rooms.
    public async Task<List<HotelSearchResultDto>> SearchAsync(HotelSearchRequestDto request)
    {
        if (request.CheckOutDate <= request.CheckInDate)
            throw new ValidationException("CheckOutDate must be after CheckInDate.");

        // Step 1: Get all active hotels at the requested destination.
        var hotels = await _db.Hotels
            .Include(h => h.Rooms)
            .Where(h => h.DestinationId == request.DestinationId && h.Status == HotelStatus.Active)
            .ToListAsync();

        var results = new List<HotelSearchResultDto>();

        foreach (var hotel in hotels)
        {
            foreach (var room in hotel.Rooms.Where(r => r.Status == RoomStatus.Active
                                                     && r.Capacity >= request.NumberOfGuests))
            {
                // Step 2: Calculate how many rooms of this type are already booked for these dates.
                int bookedCount = await CountBookedRoomsAsync(room.Id, request.CheckInDate, request.CheckOutDate);
                int available   = room.TotalRooms - bookedCount;

                if (available <= 0) continue;

                // Step 3: Apply budget filter if provided.
                if (request.MaxBudgetPerNight.HasValue && room.PricePerNight > request.MaxBudgetPerNight.Value)
                    continue;

                results.Add(new HotelSearchResultDto
                {
                    HotelId            = hotel.Id,
                    HotelName          = hotel.Name,
                    StarRating         = hotel.StarRating,
                    RoomId             = room.Id,
                    RoomType           = room.RoomType,
                    PricePerNight      = room.PricePerNight,
                    AvailableRoomCount = available
                });
            }
        }

        // Step 4: Rank results.
        // Budget-friendly rooms first, then higher star rating, then lower price as tiebreaker.
        bool WithinBudget(HotelSearchResultDto r) =>
            !request.MaxBudgetPerNight.HasValue || r.PricePerNight <= request.MaxBudgetPerNight.Value;

        results = results
            .OrderByDescending(r => WithinBudget(r))           // within budget first
            .ThenByDescending(r => r.StarRating ?? 0)          // higher stars next
            .ThenBy(r => r.PricePerNight)                      // cheaper as tiebreaker
            .ToList();

        return results;
    }

    // Public hotel detail — only Active hotels are visible to travelers.
    // The owner and admins can still see any status.
    public async Task<HotelDetailDto> GetPublicHotelByIdAsync(
        int hotelId, int requestingUserId, string requestingUserRole)
    {
        var hotel = await GetHotelOrThrowAsync(hotelId);

        bool isOwner = hotel.OwnerId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        // Non-owners and non-admins can only see Active hotels.
        if (!isOwner && !isAdmin && hotel.Status != HotelStatus.Active)
            throw new NotFoundException($"Hotel with ID {hotelId} was not found.");

        return await LoadHotelDetailAsync(hotelId);
    }

    // ── Shared helper: overlap availability check ────────────────────────────

    // Counts how many rooms of a given type are already booked (Held or Confirmed)
    // for any date range that overlaps with [checkIn, checkOut).
    //
    // This same logic is reused by SearchAsync (above) and BookingService.CreateAsync
    // (to validate that a new booking won't exceed TotalRooms).
    //
    // Two date ranges overlap if: startA < endB AND startB < endA.
    public async Task<int> CountBookedRoomsAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var booked = await _db.Bookings
            .Where(b => b.RoomId == roomId
                     && (b.Status == BookingStatus.Held || b.Status == BookingStatus.Confirmed)
                     && b.CheckInDate  < checkOut   // overlap condition part 1
                     && b.CheckOutDate > checkIn)   // overlap condition part 2
            .SumAsync(b => (int?)b.NumberOfRooms) ?? 0;

        return booked;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Loads a hotel with Owner, Destination, and Rooms fully populated.
    private async Task<HotelDetailDto> LoadHotelDetailAsync(int hotelId)
    {
        var hotel = await _db.Hotels
            .Include(h => h.Owner)
            .Include(h => h.Destination)
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == hotelId);

        if (hotel == null)
            throw new NotFoundException($"Hotel with ID {hotelId} was not found.");

        return hotel.ToDetailDto();
    }

    // Fetches a Hotel by ID or throws NotFoundException.
    private async Task<Hotel> GetHotelOrThrowAsync(int hotelId)
    {
        var hotel = await _db.Hotels.FindAsync(hotelId);
        if (hotel == null)
            throw new NotFoundException($"Hotel with ID {hotelId} was not found.");
        return hotel;
    }

    // Fetches a Room by ID, confirms it belongs to the given hotel.
    private async Task<Room> GetRoomOrThrowAsync(int roomId, int hotelId)
    {
        var room = await _db.Rooms.FindAsync(roomId);
        if (room == null || room.HotelId != hotelId)
            throw new NotFoundException($"Room with ID {roomId} was not found on hotel {hotelId}.");
        return room;
    }

    // Throws ForbiddenException if the requesting user doesn't own this hotel.
    private static void CheckOwner(Hotel hotel, int requestingUserId)
    {
        if (hotel.OwnerId != requestingUserId)
            throw new ForbiddenException("You do not have permission to manage this hotel.");
    }
}
