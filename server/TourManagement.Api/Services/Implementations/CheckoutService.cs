using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Exceptions;
using TourManagement.Api.Data;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Mappings;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;

using ValidationException = TourManagement.Api.Common.Exceptions.ValidationException;

namespace TourManagement.Api.Services.Implementations;

public class CheckoutService : ICheckoutService
{
    private readonly AppDbContext _db;
    private readonly IHotelService _hotelService;
    private readonly IVehicleService _vehicleService;

    // How long a hold is kept before it expires.
    private static readonly TimeSpan HoldDuration = TimeSpan.FromHours(12);

    public CheckoutService(AppDbContext db, IHotelService hotelService, IVehicleService vehicleService)
    {
        _db             = db;
        _hotelService   = hotelService;
        _vehicleService = vehicleService;
    }

    // ── PlaceHoldAsync ────────────────────────────────────────────────────────
    // Core operation.  Creates a HotelBooking, VehicleBooking, or both — and a
    // TripCheckout linking them — inside a single database transaction, protected
    // by PostgreSQL advisory locks so two simultaneous requests cannot both take
    // the last available room or vehicle.
    public async Task<CheckoutResponseDto> PlaceHoldAsync(CreateCheckoutDto dto, int travelerId)
    {
        return await PlaceHoldInternalAsync(dto, travelerId, isAgenticProposal: false);
    }

    public async Task<CheckoutResponseDto> PlaceApprovedProposalHoldAsync(CreateCheckoutDto dto, int travelerId)
    {
        return await PlaceHoldInternalAsync(dto, travelerId, isAgenticProposal: true);
    }

    private async Task<CheckoutResponseDto> PlaceHoldInternalAsync(CreateCheckoutDto dto, int travelerId, bool isAgenticProposal)
    {
        var allHotels = dto.Hotels.ToList();
        if (dto.Hotel != null) allHotels.Add(dto.Hotel);

        if (!allHotels.Any() && dto.Vehicle == null)
            throw new ValidationException("At least one item (Hotel or Vehicle) must be selected.");

        foreach (var h in allHotels)
        {
            h.CheckInDate = DateTime.SpecifyKind(h.CheckInDate, DateTimeKind.Utc);
            h.CheckOutDate = DateTime.SpecifyKind(h.CheckOutDate, DateTimeKind.Utc);
        }
        if (dto.Vehicle != null)
        {
            dto.Vehicle.StartDate = DateTime.SpecifyKind(dto.Vehicle.StartDate, DateTimeKind.Utc);
            dto.Vehicle.EndDate = DateTime.SpecifyKind(dto.Vehicle.EndDate, DateTimeKind.Utc);
        }

        var trip = await _db.Trips.Include(t => t.ItineraryItems).FirstOrDefaultAsync(t => t.Id == dto.TripId);
        if (trip == null) throw new NotFoundException($"Trip with ID {dto.TripId} was not found.");
        if (trip.TravelerId != travelerId) throw new ForbiddenException("You can only place holds for your own trips.");

        if (isAgenticProposal)
        {
            if (string.IsNullOrWhiteSpace(dto.ProposalId))
                throw new ValidationException("A ProposalId is required for agentic holds.");

            var existingCheckout = await _db.TripCheckouts
                .Include(c => c.HotelBookings)
                .Include(c => c.VehicleBooking)
                .FirstOrDefaultAsync(c => c.ProposalId == dto.ProposalId);
            if (existingCheckout != null)
            {
                if (!AreProposalsEquivalent(existingCheckout, dto, allHotels))
                    throw new ValidationException("A checkout with this ProposalId already exists but contains different items or dates.");
                return await LoadCheckoutDtoAsync(existingCheckout.Id);
            }

            await ValidateAgenticProposalAsync(dto, trip);
        }

        var now = DateTime.UtcNow;

        if (!isAgenticProposal)
        {
            var existingCheckout = await FindDuplicateActiveCheckoutAsync(dto, travelerId, now);
            if (existingCheckout != null)
                return await LoadCheckoutDtoAsync(existingCheckout.Id);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (_db.Database.IsRelational())
            {
                var sortedRoomIds = allHotels.Select(h => h.RoomId).OrderBy(id => id).Distinct().ToList();
                foreach (var rId in sortedRoomIds)
                {
                    long roomLockKey = BuildLockKey("room", rId);
                    await _db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({roomLockKey})");
                }

                if (dto.Vehicle != null)
                {
                    long vehicleLockKey = BuildLockKey("vehicle", dto.Vehicle.VehicleId);
                    await _db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({vehicleLockKey})");
                }
            }

            if (isAgenticProposal && !string.IsNullOrWhiteSpace(dto.ProposalId))
            {
                var concurrentCheckout = await _db.TripCheckouts.FirstOrDefaultAsync(c => c.ProposalId == dto.ProposalId);
                if (concurrentCheckout != null)
                {
                    await transaction.CommitAsync();
                    return await LoadCheckoutDtoAsync(concurrentCheckout.Id);
                }
            }

            var hotelBookings = new List<HotelBooking>();
            decimal totalHotelPrice = 0m;
            foreach (var h in allHotels)
            {
                var (hb, snapshot) = await ValidateAndCreateHotelBookingAsync(h, trip, travelerId);
                hb.PriceSnapshot = snapshot;
                hotelBookings.Add(hb);
                totalHotelPrice += snapshot;
            }

            VehicleBooking? vehicleBooking = null;
            decimal vehicleSnapshot = 0m;
            if (dto.Vehicle != null)
            {
                var (vb, vSnapshot) = await ValidateAndCreateVehicleBookingAsync(dto.Vehicle, trip, travelerId);
                vehicleBooking = vb;
                vehicleSnapshot = vSnapshot;
            }

            var expiresAt = now.Add(HoldDuration);
            var checkout = new TripCheckout
            {
                TripId = dto.TripId,
                TravelerId = travelerId,
                HotelPriceSnapshot = hotelBookings.Any() ? totalHotelPrice : null,
                VehiclePriceSnapshot = vehicleBooking != null ? vehicleSnapshot : null,
                TotalPrice = totalHotelPrice + vehicleSnapshot,
                Status = CheckoutStatus.Active,
                ProposalId = isAgenticProposal ? dto.ProposalId : null,
                HoldExpiresAt = expiresAt,
                CreatedAt = now,
                UpdatedAt = now,
            };

            foreach (var hb in hotelBookings)
            {
                hb.HoldExpiresAt = expiresAt;
                checkout.HotelBookings.Add(hb);
            }
            if (vehicleBooking != null)
            {
                vehicleBooking.HoldExpiresAt = expiresAt;
                checkout.VehicleBooking = vehicleBooking;
            }

            _db.TripCheckouts.Add(checkout);
            await _db.SaveChangesAsync(); 

            if (hotelBookings.Any())
                checkout.HotelBookingId = hotelBookings.First().Id;
            checkout.VehicleBookingId = vehicleBooking?.Id;

            foreach (var hb in hotelBookings) hb.CheckoutId = checkout.Id;
            if (vehicleBooking != null) vehicleBooking.CheckoutId = checkout.Id;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await LoadCheckoutDtoAsync(checkout.Id);
        }
        catch (DbUpdateException dbEx) when (isAgenticProposal && dbEx.InnerException?.Message.Contains("IX_TripCheckouts_ProposalId") == true)
        {
            await transaction.RollbackAsync();
            var winner = await _db.TripCheckouts.FirstOrDefaultAsync(c => c.ProposalId == dto.ProposalId);
            if (winner != null)
                return await LoadCheckoutDtoAsync(winner.Id);
            throw;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private bool AreProposalsEquivalent(TripCheckout checkout, CreateCheckoutDto dto, List<HotelCheckoutItemDto> dtoHotels)
    {
        if (checkout.TripId != dto.TripId) return false;
        if (checkout.HotelBookings.Count != dtoHotels.Count) return false;

        var checkoutHotels = checkout.HotelBookings.OrderBy(h => h.CheckInDate).ToList();
        var sortedDtoHotels = dtoHotels.OrderBy(h => h.CheckInDate).ToList();

        for (int i = 0; i < checkoutHotels.Count; i++)
        {
            var h1 = checkoutHotels[i];
            var h2 = sortedDtoHotels[i];
            if (h1.RoomId != h2.RoomId || h1.NumberOfRooms != h2.NumberOfRooms || 
                h1.CheckInDate.Date != h2.CheckInDate.Date || h1.CheckOutDate.Date != h2.CheckOutDate.Date)
            {
                return false;
            }
        }

        if (checkout.VehicleBookingId.HasValue != (dto.Vehicle != null)) return false;
        if (checkout.VehicleBookingId.HasValue && dto.Vehicle != null)
        {
            var v1 = checkout.VehicleBooking;
            var v2 = dto.Vehicle;
            if (v1!.VehicleId != v2.VehicleId || v1.StartDate.Date != v2.StartDate.Date || v1.EndDate.Date != v2.EndDate.Date)
                return false;
        }

        return true;
    }

    public async Task ValidateAgenticProposalAsync(CreateCheckoutDto dto, Trip trip)
    {
        var allHotels = dto.Hotels?.ToList() ?? new List<HotelCheckoutItemDto>();
        if (dto.Hotel != null && !allHotels.Any(h => h.RoomId == dto.Hotel.RoomId))
        {
            allHotels.Add(dto.Hotel);
        }

        var tripDestinationIds = trip.ItineraryItems.Select(i => i.DestinationId).ToHashSet();
            
        if (trip.StartDate.Date == trip.EndDate.Date)
        {
            if (allHotels.Any())
                throw new ValidationException("A one-day trip cannot have hotel stays.");
        }
        else
        {
            if (!allHotels.Any())
                throw new ValidationException("An empty hotel list is not permitted when the trip requires hotel stays.");

            var roomIds = allHotels.Select(h => h.RoomId).ToList();
            var rooms = await _db.Rooms.Include(r => r.Hotel).Where(r => roomIds.Contains(r.Id)).ToListAsync();
                
            var areaMap = new HashSet<int>();
            foreach (var h in allHotels)
            {
                var room = rooms.FirstOrDefault(r => r.Id == h.RoomId);
                if (room == null) throw new NotFoundException("Room not found.");
                    
                var destId = room.Hotel.DestinationId;
                if (!tripDestinationIds.Contains(destId))
                    throw new ValidationException($"Proposed hotel '{room.Hotel.Name}' is in an area not selected for this trip.");
                    
                if (areaMap.Contains(destId))
                    throw new ValidationException($"Cannot propose multiple separate hotel stays for the same area.");
                areaMap.Add(destId);
            }

            var sortedHotels = allHotels.OrderBy(h => h.CheckInDate).ToList();
            for (int i = 0; i < sortedHotels.Count; i++)
            {
                var h = sortedHotels[i];
                if (h.CheckInDate.Date >= h.CheckOutDate.Date)
                    throw new ValidationException("Each hotel stay must contain at least one night.");

                if (i == 0 && h.CheckInDate.Date != trip.StartDate.Date)
                    throw new ValidationException("The first hotel stay must check in on the trip StartDate.");
                    
                if (i == sortedHotels.Count - 1 && h.CheckOutDate.Date != trip.EndDate.Date)
                    throw new ValidationException("The final hotel stay must check out on the trip EndDate.");
                    
                if (i < sortedHotels.Count - 1 && h.CheckOutDate.Date != sortedHotels[i + 1].CheckInDate.Date)
                    throw new ValidationException("Hotel stays must be contiguous with no gaps or overlaps.");
            }
        }
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────
    public async Task<CheckoutResponseDto> GetByIdAsync(int checkoutId, int travelerId)
    {
        var checkout = await _db.TripCheckouts.FindAsync(checkoutId);
        if (checkout == null)
            throw new NotFoundException($"Checkout with ID {checkoutId} was not found.");
        if (checkout.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to view this checkout.");

        return await LoadCheckoutDtoAsync(checkoutId);
    }

    // ── GetMyCheckoutsAsync ───────────────────────────────────────────────────
    public async Task<PagedResult<CheckoutResponseDto>> GetMyCheckoutsAsync(
        int travelerId, int page, int pageSize)
    {
        var query = _db.TripCheckouts
            .Where(c => c.TravelerId == travelerId)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync();
        var checkoutIds = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => c.Id)
            .ToListAsync();

        var items = new List<CheckoutResponseDto>();
        foreach (var id in checkoutIds)
            items.Add(await LoadCheckoutDtoAsync(id));

        return new PagedResult<CheckoutResponseDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────
    public async Task CancelAsync(int checkoutId, int travelerId)
    {
        var checkout = await _db.TripCheckouts.FindAsync(checkoutId);
        if (checkout == null)
            throw new NotFoundException($"Checkout with ID {checkoutId} was not found.");
        if (checkout.TravelerId != travelerId)
            throw new ForbiddenException("You do not have permission to cancel this checkout.");
        if (checkout.Status == CheckoutStatus.Cancelled)
            throw new ValidationException("This checkout is already cancelled.");
        if (checkout.Status == CheckoutStatus.Paid)
            throw new ValidationException("A paid checkout cannot be cancelled through this endpoint.");

        var now = DateTime.UtcNow;

        // Cancel the linked hotel bookings if still Held.
        var hotelBookings = await _db.HotelBookings.Where(b => b.CheckoutId == checkoutId).ToListAsync();
        foreach (var hb in hotelBookings)
        {
            if (hb.Status == BookingStatus.Held)
            {
                hb.Status    = BookingStatus.Cancelled;
                hb.UpdatedAt = now;
            }
        }

        // Cancel the linked vehicle booking if still Held.
        if (checkout.VehicleBookingId.HasValue)
        {
            var vb = await _db.VehicleBookings.FindAsync(checkout.VehicleBookingId.Value);
            if (vb != null && vb.Status == BookingStatus.Held)
            {
                vb.Status    = BookingStatus.Cancelled;
                vb.UpdatedAt = now;
            }
        }

        checkout.Status    = CheckoutStatus.Cancelled;
        checkout.UpdatedAt = now;
        await _db.SaveChangesAsync();
    }

    // ── ConfirmAsync ──────────────────────────────────────────────────────────
    // Reserved for the future PayHere callback.  See ICheckoutService for details.
    // Do NOT call from any UI action or Traveler-accessible endpoint in this task.
    public async Task ConfirmAsync(int checkoutId)
    {
        // ── PayHere integration point ──────────────────────────────────────────────
        // When PayHere integration is implemented (separate task):
        //
        // 1. POST /api/checkouts/{id}/initiate-payment will:
        //    - Load the checkout (must be Active and not expired).
        //    - Call the PayHere SDK to generate a payment URL.
        //    - Return the URL to the frontend.
        //
        // 2. PayHere will POST a callback to POST /api/payhere/notify (a new
        //    UNAUTHENTICATED endpoint verified by HMAC using PayHereSettings.MerchantSecret).
        //
        // 3. That callback handler calls this method after verifying:
        //    - The payment amount matches checkout.TotalPrice.
        //    - The PayHere status is "2" (success).
        //
        // 4. This method transitions: Checkout → Paid, HotelBooking → Confirmed,
        //    VehicleBooking → Confirmed.
        //
        // Do NOT call ConfirmAsync from any UI button or automatic code path.
        // ──────────────────────────────────────────────────────────────────────────

        var checkout = await _db.TripCheckouts.FindAsync(checkoutId);
        if (checkout == null)
            throw new NotFoundException($"Checkout with ID {checkoutId} was not found.");
        if (checkout.Status != CheckoutStatus.Active)
            throw new ValidationException($"Cannot confirm a checkout that is {checkout.Status}.");
        if (checkout.HoldExpiresAt < DateTime.UtcNow)
            throw new ValidationException("This hold has expired. The traveler must place a new hold before payment can be accepted.");

        var now = DateTime.UtcNow;

        var hotelBookings = await _db.HotelBookings.Where(b => b.CheckoutId == checkoutId).ToListAsync();
        foreach (var hb in hotelBookings)
        {
            hb.Status    = BookingStatus.Confirmed;
            hb.UpdatedAt = now;
        }

        if (checkout.VehicleBookingId.HasValue)
        {
            var vb = await _db.VehicleBookings.FindAsync(checkout.VehicleBookingId.Value);
            if (vb != null)
            {
                vb.Status    = BookingStatus.Confirmed;
                vb.UpdatedAt = now;
            }
        }

        checkout.Status    = CheckoutStatus.Paid;
        checkout.UpdatedAt = now;
        await _db.SaveChangesAsync();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Validates the hotel selection, checks availability, creates the HotelBooking row,
    // and returns the row plus the price snapshot.
    // Called inside the advisory-locked transaction, so no other session can interleave.
    private async Task<(HotelBooking booking, decimal snapshot)> ValidateAndCreateHotelBookingAsync(
        HotelCheckoutItemDto item, Trip trip, int travelerId)
    {
        if (item.CheckOutDate <= item.CheckInDate)
            throw new ValidationException("Hotel CheckOutDate must be after CheckInDate.");

        if (item.CheckInDate.Date < trip.StartDate.Date || item.CheckOutDate.Date > trip.EndDate.Date)
            throw new ValidationException(
                $"Hotel booking dates must fall within the trip's date range " +
                $"({trip.StartDate:yyyy-MM-dd} – {trip.EndDate:yyyy-MM-dd}).");

        var room = await _db.Rooms.Include(r => r.Hotel).FirstOrDefaultAsync(r => r.Id == item.RoomId);
        if (room == null)
            throw new NotFoundException($"Room with ID {item.RoomId} was not found.");
        if (room.Status != RoomStatus.Active)
            throw new ValidationException("This room type is not currently available for booking.");
        if (room.Hotel.Status != HotelStatus.Active)
            throw new ValidationException("This hotel is not currently accepting bookings.");
        if (room.Capacity < 1)
            throw new ValidationException("This room has no capacity configured.");

        // Count only non-expired holds and confirmed bookings.
        int alreadyBooked = await _hotelService.CountBookedRoomsAsync(
            item.RoomId, item.CheckInDate, item.CheckOutDate);

        int available = room.TotalRooms - alreadyBooked;
        if (item.NumberOfRooms > available)
            throw new ValidationException(
                $"Not enough rooms available. Requested: {item.NumberOfRooms}, available: {available}.");

        // Compute price snapshot: price per night × nights × rooms.
        int nights = (int)(item.CheckOutDate.Date - item.CheckInDate.Date).TotalDays;
        decimal snapshot = room.PricePerNight * nights * item.NumberOfRooms;

        var now = DateTime.UtcNow;
        var booking = new HotelBooking
        {
            TripId        = trip.Id,
            RoomId        = item.RoomId,
            CheckInDate   = item.CheckInDate,
            CheckOutDate  = item.CheckOutDate,
            NumberOfRooms = item.NumberOfRooms,
            Status        = BookingStatus.Held,
            // HoldExpiresAt and CheckoutId are set after the checkout record is saved
            // (we need the checkout ID first).
            CreatedAt     = now,
            UpdatedAt     = now,
        };
        
        return (booking, snapshot);
    }

    // Validates the vehicle selection, checks availability, creates the VehicleBooking row,
    // and returns the row plus the price snapshot.
    private async Task<(VehicleBooking booking, decimal snapshot)> ValidateAndCreateVehicleBookingAsync(
        VehicleCheckoutItemDto item, Trip trip, int travelerId)
    {
        if (item.EndDate <= item.StartDate)
            throw new ValidationException("Vehicle EndDate must be after StartDate.");

        if (item.StartDate.Date < trip.StartDate.Date || item.EndDate.Date > trip.EndDate.Date)
            throw new ValidationException(
                $"Vehicle booking dates must fall within the trip's date range " +
                $"({trip.StartDate:yyyy-MM-dd} – {trip.EndDate:yyyy-MM-dd}).");

        var vehicle = await _db.Vehicles.FindAsync(item.VehicleId);
        if (vehicle == null)
            throw new NotFoundException($"Vehicle with ID {item.VehicleId} was not found.");
        if (vehicle.Status != VehicleStatus.Active)
            throw new ValidationException("This vehicle is not currently available for booking.");

        bool available = await _vehicleService.IsVehicleAvailableAsync(
            item.VehicleId, item.StartDate, item.EndDate);

        if (!available)
            throw new ValidationException(
                "This vehicle is already booked for an overlapping date range. Please choose different dates.");

        // Compute price snapshot: price per day × days.
        int days = (int)(item.EndDate.Date - item.StartDate.Date).TotalDays;
        decimal snapshot = vehicle.PricePerDay * days;

        var now = DateTime.UtcNow;
        var booking = new VehicleBooking
        {
            TripId          = trip.Id,
            VehicleId       = item.VehicleId,
            StartDate       = item.StartDate,
            EndDate         = item.EndDate,
            PickupLatitude  = item.PickupLatitude,
            PickupLongitude = item.PickupLongitude,
            PickupNote      = item.PickupNote,
            Status          = BookingStatus.Held,
            CreatedAt       = now,
            UpdatedAt       = now,
        };
        
        return (booking, snapshot);
    }

    // Loads a TripCheckout with all its related data and maps it to the response DTO.
    private async Task<CheckoutResponseDto> LoadCheckoutDtoAsync(int checkoutId)
    {
        var c = await _db.TripCheckouts
            .FirstOrDefaultAsync(co => co.Id == checkoutId);

        if (c == null)
            throw new NotFoundException($"Checkout with ID {checkoutId} was not found.");

        CheckoutResponseDto dto = c.ToResponseDto();

        var hotelBookings = await _db.HotelBookings
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .Where(b => b.CheckoutId == c.Id)
            .ToListAsync();

        foreach (var hb in hotelBookings)
        {
            var itemDto = hb.ToHoldItemDto(hb.PriceSnapshot ?? c.HotelPriceSnapshot ?? 0m);
            dto.Hotels.Add(itemDto);
        }

        if (dto.Hotels.Any())
        {
            dto.HotelItem = dto.Hotels.First();
        }

        if (c.VehicleBookingId.HasValue)
        {
            var vb = await _db.VehicleBookings
                .Include(b => b.Vehicle)
                .FirstOrDefaultAsync(b => b.Id == c.VehicleBookingId.Value);

            if (vb != null)
                dto.VehicleItem = vb.ToHoldItemDto(c.VehiclePriceSnapshot ?? 0m);
        }

        return dto;
    }

    // Returns an existing Active checkout with the same trip + hotel room + vehicle params.
    // Used for idempotency — a network retry with the same params gets the same checkout back.
    private async Task<TripCheckout?> FindDuplicateActiveCheckoutAsync(
        CreateCheckoutDto dto, int travelerId, DateTime now)
    {
        var query = _db.TripCheckouts
            .Where(c => c.TripId    == dto.TripId
                     && c.TravelerId == travelerId
                     && c.Status    == CheckoutStatus.Active
                     && c.HoldExpiresAt > now);

        if (dto.Hotel != null)
        {
            query = query.Where(c => c.HotelBookingId != null);
        }
        else
        {
            query = query.Where(c => c.HotelBookingId == null);
        }

        if (dto.Vehicle != null)
        {
            query = query.Where(c => c.VehicleBookingId != null);
        }
        else
        {
            query = query.Where(c => c.VehicleBookingId == null);
        }

        // If hotel params are provided, join to the hotel booking to match on RoomId and dates.
        if (dto.Hotel != null)
        {
            var roomId       = dto.Hotel.RoomId;
            var checkIn      = dto.Hotel.CheckInDate;
            var checkOut     = dto.Hotel.CheckOutDate;
            var numRooms     = dto.Hotel.NumberOfRooms;

            return await query
                .Join(_db.HotelBookings,
                      c  => c.HotelBookingId,
                      hb => (int?)hb.Id,
                      (c, hb) => new { c, hb })
                .Where(x => x.hb.RoomId       == roomId
                          && x.hb.CheckInDate  == checkIn
                          && x.hb.CheckOutDate == checkOut
                          && x.hb.NumberOfRooms == numRooms)
                .Select(x => x.c)
                .FirstOrDefaultAsync();
        }

        if (dto.Vehicle != null)
        {
            var vehicleId = dto.Vehicle.VehicleId;
            var startDate = dto.Vehicle.StartDate;
            var endDate   = dto.Vehicle.EndDate;

            return await query
                .Join(_db.VehicleBookings,
                      c  => c.VehicleBookingId,
                      vb => (int?)vb.Id,
                      (c, vb) => new { c, vb })
                .Where(x => x.vb.VehicleId == vehicleId
                          && x.vb.StartDate == startDate
                          && x.vb.EndDate   == endDate)
                .Select(x => x.c)
                .FirstOrDefaultAsync();
        }

        return await query.FirstOrDefaultAsync();
    }

    // Generates a stable lock key for a given resource type and ID.
    // The key is a long that PostgreSQL uses for pg_advisory_xact_lock.
    private static long BuildLockKey(string resourceType, int resourceId)
    {
        // Prefix the resource type with a small hash so "room:1" and "vehicle:1"
        // get different lock keys.
        int typeHash = resourceType switch
        {
            "room"    => 1_000_000,
            "vehicle" => 2_000_000,
            _         => resourceId.GetHashCode()
        };
        return (long)typeHash + resourceId;
    }
}
