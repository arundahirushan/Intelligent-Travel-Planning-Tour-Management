using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class TripService : ITripService
{
    private readonly AppDbContext _db;

    public TripService(AppDbContext db)
    {
        _db = db;
    }

    // Create a new trip for the logged-in traveler. Starts in Draft status.
    public async Task<TripDetailDto> CreateAsync(CreateTripDto dto, int travelerId)
    {
        ValidateTripDates(dto.StartDate, dto.EndDate);

        var trip = new Trip
        {
            TravelerId = travelerId,
            Title      = dto.Title,
            StartDate  = dto.StartDate,
            EndDate    = dto.EndDate,
            Budget     = dto.Budget,
            GroupSize  = dto.GroupSize,
            Interests  = dto.Interests,
            Status     = TripStatus.Draft,
            CreatedAt  = DateTime.UtcNow,
            UpdatedAt  = DateTime.UtcNow
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        // Return a full detail DTO (itinerary will be empty at this point).
        return await LoadTripDetailAsync(trip.Id);
    }

    // Get the logged-in traveler's own trips with optional filtering and paging.
    public async Task<PagedResult<TripSummaryDto>> GetMyTripsAsync(
        int travelerId, string? status, string? search, string? sort, int page, int pageSize)
    {
        var query = _db.Trips.Where(t => t.TravelerId == travelerId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TripStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Title.Contains(search));

        query = sort switch
        {
            "startDate" => query.OrderBy(t => t.StartDate),
            "budget"    => query.OrderByDescending(t => t.Budget),
            "oldest"    => query.OrderBy(t => t.CreatedAt),
            _           => query.OrderByDescending(t => t.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<TripSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // Get full trip details including all itinerary items, sorted by day then sequence.
    // Only the owner or an Admin/SuperAdmin can view this.
    public async Task<TripDetailDto> GetByIdAsync(int id, int requestingUserId, string requestingUserRole)
    {
        var trip = await LoadTripDetailAsync(id);  // throws NotFoundException if missing

        // Re-fetch the raw trip to check ownership.
        var rawTrip = await _db.Trips.FindAsync(id);
        CheckOwnerOrAdmin(rawTrip!, requestingUserId, requestingUserRole);

        return trip;
    }

    // Update a trip's details. Only allowed while the trip is still in Draft status.
    public async Task<TripDetailDto> UpdateAsync(int id, UpdateTripDto dto, int requestingUserId)
    {
        var trip = await GetTripOrThrowAsync(id);
        CheckOwnerOrAdmin(trip, requestingUserId, Roles.Traveler);  // traveler-only operation

        // Only Draft trips can be edited — once the status advances, the plan is locked.
        if (trip.Status != TripStatus.Draft)
            throw new ValidationException("Trip can only be edited while in Draft status.");

        ValidateTripDates(dto.StartDate, dto.EndDate);

        trip.Title     = dto.Title;
        trip.StartDate = dto.StartDate;
        trip.EndDate   = dto.EndDate;
        trip.Budget    = dto.Budget;
        trip.GroupSize = dto.GroupSize;
        trip.Interests = dto.Interests;
        trip.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await LoadTripDetailAsync(trip.Id);
    }

    // Cancel a trip (traveler action). Only allowed if Draft or Planned.
    // We set Status = Cancelled instead of deleting, so history is preserved.
    public async Task CancelAsync(int id, int requestingUserId)
    {
        var trip = await GetTripOrThrowAsync(id);
        CheckOwnerOrAdmin(trip, requestingUserId, Roles.Traveler);

        if (trip.Status != TripStatus.Draft && trip.Status != TripStatus.Planned)
            throw new ValidationException("Trips can only be cancelled while in Draft or Planned status.");

        trip.Status    = TripStatus.Cancelled;
        trip.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Admin override — force-cancel any trip regardless of its current status.
    public async Task ForceCancelAsync(int id)
    {
        var trip = await GetTripOrThrowAsync(id);
        trip.Status    = TripStatus.Cancelled;
        trip.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Admin view — all trips across all travelers with filtering, search, sort, paging.
    public async Task<PagedResult<TripSummaryDto>> GetAllAsync(
        string? status, int? destinationId, int? travelerId,
        string? search, string? sort, int page, int pageSize)
    {
        var query = _db.Trips.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TripStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        if (travelerId.HasValue)
            query = query.Where(t => t.TravelerId == travelerId.Value);

        // Filter by destination: only trips that have an itinerary item for that destination.
        if (destinationId.HasValue)
            query = query.Where(t => t.ItineraryItems.Any(i => i.DestinationId == destinationId.Value));

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Title.Contains(search));

        query = sort switch
        {
            "startDate" => query.OrderBy(t => t.StartDate),
            "budget"    => query.OrderByDescending(t => t.Budget),
            _           => query.OrderByDescending(t => t.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.ToSummaryDto())
            .ToListAsync();

        return new PagedResult<TripSummaryDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    // Add a single itinerary item to a trip.
    public async Task<TripDetailDto> AddItineraryItemAsync(int tripId, CreateItineraryItemDto dto, int requestingUserId)
    {
        var trip = await GetTripOrThrowAsync(tripId);
        CheckOwnerOrAdmin(trip, requestingUserId, Roles.Traveler);

        // Verify the destination actually exists.
        bool destExists = await _db.Destinations.AnyAsync(d => d.Id == dto.DestinationId);
        if (!destExists)
            throw new NotFoundException($"Destination with ID {dto.DestinationId} was not found.");

        ValidateDayNumber(dto.DayNumber, trip);

        var item = new ItineraryItem
        {
            TripId        = tripId,
            DestinationId = dto.DestinationId,
            DayNumber     = dto.DayNumber,
            Notes         = dto.Notes,
            SequenceOrder = dto.SequenceOrder,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };

        _db.ItineraryItems.Add(item);
        await _db.SaveChangesAsync();
        return await LoadTripDetailAsync(tripId);
    }

    // Edit an existing itinerary item.
    public async Task<TripDetailDto> UpdateItineraryItemAsync(
        int tripId, int itemId, UpdateItineraryItemDto dto, int requestingUserId)
    {
        var trip = await GetTripOrThrowAsync(tripId);
        CheckOwnerOrAdmin(trip, requestingUserId, Roles.Traveler);

        var item = await _db.ItineraryItems.FindAsync(itemId);
        if (item == null || item.TripId != tripId)
            throw new NotFoundException($"Itinerary item with ID {itemId} was not found on trip {tripId}.");

        // Verify the destination exists.
        bool destExists = await _db.Destinations.AnyAsync(d => d.Id == dto.DestinationId);
        if (!destExists)
            throw new NotFoundException($"Destination with ID {dto.DestinationId} was not found.");

        ValidateDayNumber(dto.DayNumber, trip);

        item.DestinationId = dto.DestinationId;
        item.DayNumber     = dto.DayNumber;
        item.Notes         = dto.Notes;
        item.SequenceOrder = dto.SequenceOrder;
        item.UpdatedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await LoadTripDetailAsync(tripId);
    }

    // Remove one itinerary item from a trip.
    public async Task<TripDetailDto> RemoveItineraryItemAsync(int tripId, int itemId, int requestingUserId)
    {
        var trip = await GetTripOrThrowAsync(tripId);
        CheckOwnerOrAdmin(trip, requestingUserId, Roles.Traveler);

        var item = await _db.ItineraryItems.FindAsync(itemId);
        if (item == null || item.TripId != tripId)
            throw new NotFoundException($"Itinerary item with ID {itemId} was not found on trip {tripId}.");

        _db.ItineraryItems.Remove(item);
        await _db.SaveChangesAsync();
        return await LoadTripDetailAsync(tripId);
    }

    // Auto-generate a draft itinerary by splitting the trip's days evenly across
    // the given destinations in the order provided.
    //
    // Example: 6-day trip, 3 destinations → 2 days each.
    // Example: 7-day trip, 3 destinations → dest1 gets 3 days, dest2 gets 2, dest3 gets 2
    //          (extra days go to the first destinations in the list).
    public async Task<TripDetailDto> GenerateDraftItineraryAsync(
        int tripId, GenerateDraftItineraryDto dto, int requestingUserId)
    {
        var trip = await GetTripOrThrowAsync(tripId);
        CheckOwnerOrAdmin(trip, requestingUserId, Roles.Traveler);

        // Validate all destination IDs exist before making any changes.
        foreach (var destId in dto.DestinationIds)
        {
            bool exists = await _db.Destinations.AnyAsync(d => d.Id == destId);
            if (!exists)
                throw new NotFoundException($"Destination with ID {destId} was not found.");
        }

        int totalDays   = (trip.EndDate.Date - trip.StartDate.Date).Days + 1;
        int destCount   = dto.DestinationIds.Count;
        int baseDays    = totalDays / destCount;   // how many days each destination gets at minimum
        int extraDays   = totalDays % destCount;   // leftover days — given to the first destinations

        // Delete any existing itinerary items so we start fresh.
        var existingItems = _db.ItineraryItems.Where(i => i.TripId == tripId);
        _db.ItineraryItems.RemoveRange(existingItems);

        // Build the new items.
        var newItems = new List<ItineraryItem>();
        int currentDay = 1;

        for (int i = 0; i < destCount; i++)
        {
            // First `extraDays` destinations get one extra day.
            int daysForThisDest = baseDays + (i < extraDays ? 1 : 0);

            for (int d = 0; d < daysForThisDest; d++)
            {
                newItems.Add(new ItineraryItem
                {
                    TripId        = tripId,
                    DestinationId = dto.DestinationIds[i],
                    DayNumber     = currentDay,
                    Notes         = null,
                    SequenceOrder = 1,  // one item per day in the auto-generated draft
                    CreatedAt     = DateTime.UtcNow,
                    UpdatedAt     = DateTime.UtcNow
                });
                currentDay++;
            }
        }

        _db.ItineraryItems.AddRange(newItems);
        trip.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await LoadTripDetailAsync(tripId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Loads a trip with its itinerary items (and each item's destination) from the DB.
    // Items are sorted by DayNumber then SequenceOrder.
    private async Task<TripDetailDto> LoadTripDetailAsync(int tripId)
    {
        var trip = await _db.Trips
            .Include(t => t.ItineraryItems
                .OrderBy(i => i.DayNumber)
                .ThenBy(i => i.SequenceOrder))
            .ThenInclude(i => i.Destination)  // needed so DestinationName is populated
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null)
            throw new NotFoundException($"Trip with ID {tripId} was not found.");

        return trip.ToDetailDto();
    }

    // Fetches a Trip by ID or throws NotFoundException.
    private async Task<Trip> GetTripOrThrowAsync(int tripId)
    {
        var trip = await _db.Trips.FindAsync(tripId);
        if (trip == null)
            throw new NotFoundException($"Trip with ID {tripId} was not found.");
        return trip;
    }

    // Checks that the requesting user either owns this trip or is an Admin/SuperAdmin.
    // Throws ForbiddenException if neither is true.
    // Note: when called for traveler-only operations (Update, Cancel, itinerary edits),
    // pass Roles.Traveler as requestingUserRole so the admin bypass doesn't apply.
    private static void CheckOwnerOrAdmin(Trip trip, int requestingUserId, string requestingUserRole)
    {
        bool isOwner = trip.TravelerId == requestingUserId;
        bool isAdmin = requestingUserRole == Roles.Admin || requestingUserRole == Roles.SuperAdmin;

        if (!isOwner && !isAdmin)
            throw new ForbiddenException("You do not have permission to access this trip.");
    }

    // Validates that EndDate is strictly after StartDate.
    private static void ValidateTripDates(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ValidationException("EndDate must be after StartDate.");
    }

    // Validates that a DayNumber falls within the trip's actual date range.
    private static void ValidateDayNumber(int dayNumber, Trip trip)
    {
        int totalDays = (trip.EndDate.Date - trip.StartDate.Date).Days + 1;
        if (dayNumber < 1 || dayNumber > totalDays)
            throw new ValidationException(
                $"DayNumber must be between 1 and {totalDays} (the total number of days in this trip).");
    }
}
