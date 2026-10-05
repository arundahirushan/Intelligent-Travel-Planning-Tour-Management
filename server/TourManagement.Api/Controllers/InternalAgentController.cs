using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Dtos.Workflows;
using TourManagement.Api.Models;
using TourManagement.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TourManagement.Api.Data;
using System.Text.Json;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/internal")]
public class InternalAgentController : ControllerBase
{
    private readonly ITripService _tripService;
    private readonly IHotelService _hotelService;
    private readonly IVehicleService _vehicleService;
    private readonly string _aiSecret;

    private readonly AppDbContext _db;

    public InternalAgentController(
        ITripService tripService, 
        IHotelService hotelService, 
        IVehicleService vehicleService, 
        AppDbContext db,
        IConfiguration config)
    {
        _tripService = tripService;
        _hotelService = hotelService;
        _vehicleService = vehicleService;
        _db = db;
        _aiSecret = config.GetValue<string>("AiServiceSettings:Secret") 
            ?? throw new InvalidOperationException("AiServiceSettings:Secret is not configured.");
    }

    private async Task<Models.TripProposal?> GetAuthorizedProposalAsync()
    {
        if (!Request.Headers.TryGetValue("X-AI-Secret", out var providedSecret) || providedSecret != _aiSecret)
            return null;

        if (!Request.Headers.TryGetValue("X-AI-ProposalId", out var proposalIdHeader))
            return null;

        var proposalId = proposalIdHeader.ToString();
        return await _db.TripProposals
            .FirstOrDefaultAsync(p => p.ProposalId == proposalId && p.Status == Models.ProposalStatus.Generating);
    }

    [HttpGet("trips/current")]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> GetTrip()
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        var trip = await _tripService.GetByIdAsync(proposal.TripId, 0, "SuperAdmin");
        return Ok(ApiResponse<TripDetailDto>.Ok(trip));
    }

    [HttpPost("hotels/search")]
    public async Task<ActionResult<ApiResponse<List<HotelSearchResultDto>>>> SearchHotels([FromBody] HotelSearchRequestDto request)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        // Enforce trip scope: hotels must be searched within the trip's destinations
        var trip = await _db.Trips.Include(t => t.ItineraryItems).FirstOrDefaultAsync(t => t.Id == proposal.TripId);
        if (trip == null || !trip.ItineraryItems.Any(i => i.DestinationId == request.DestinationId))
            return BadRequest(ApiResponse<List<HotelSearchResultDto>>.Fail("Requested destination is not part of the current trip."));

        var results = await _hotelService.SearchAsync(request);
        return Ok(ApiResponse<List<HotelSearchResultDto>>.Ok(results));
    }

    [HttpPost("vehicles/search")]
    public async Task<ActionResult<ApiResponse<List<VehicleSearchResultDto>>>> SearchVehicles([FromBody] VehicleSearchRequestDto request)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        var results = await _vehicleService.SearchAsync(request);
        return Ok(ApiResponse<List<VehicleSearchResultDto>>.Ok(results));
    }
    
    [HttpGet("weather")]
    public async Task<ActionResult<ApiResponse<object>>> GetWeather([FromQuery] string destination, [FromQuery] DateTime date, [FromServices] IWeatherService weatherService)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        var result = await weatherService.GetWeatherAsync(destination, date);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// M4 calls this to run all authoritative deterministic checks before Gemini review.
    /// Authentication: X-AI-Secret + X-AI-ProposalId (Generating state required).
    /// Read-only — no holds, no bookings, no SaveChanges.
    /// </summary>
    [HttpPost("proposals/validate")]
    public async Task<ActionResult<ApiResponse<ValidateProposalResultDto>>> ValidateProposal(
        [FromBody] ValidateProposalRequestDto request)
    {
        var proposal = await GetAuthorizedProposalAsync();
        if (proposal == null) return Unauthorized();

        var trip = await _db.Trips
            .Include(t => t.ItineraryItems)
            .FirstOrDefaultAsync(t => t.Id == proposal.TripId);

        if (trip == null)
            return BadRequest(ApiResponse<ValidateProposalResultDto>.Fail("Trip not found for this proposal."));

        var result = await RunValidationAsync(request, proposal, trip);
        return Ok(ApiResponse<ValidateProposalResultDto>.Ok(result));
    }

    // ── Private validation logic ─────────────────────────────────────────────
    // All checks are read-only. The method collects every issue rather than
    // short-circuiting so M4 gets a complete picture.

    private async Task<ValidateProposalResultDto> RunValidationAsync(
        ValidateProposalRequestDto req,
        Models.TripProposal proposal,
        Trip trip)
    {
        var result = new ValidateProposalResultDto
        {
            Budget   = trip.Budget,
            Currency = "LKR"
        };

        // 1. Stale-input check — compare saved snapshot to current trip fields.
        DetectStaleInputs(proposal, trip, result);

        bool isOneDayTrip = trip.StartDate.Date == trip.EndDate.Date;

        // 2. Hotel checks
        decimal accommodationCost = await ValidateHotelsAsync(req, trip, isOneDayTrip, result);
        result.AccommodationCost = accommodationCost;

        // 3. Vehicle checks
        decimal transportCost = await ValidateVehicleAsync(req, trip, proposal, result);
        result.TransportCost = transportCost;

        // 4. Budget check (only when individual checks passed enough to have prices)
        result.TotalCost = accommodationCost + transportCost;
        if (result.TotalCost > trip.Budget)
        {
            result.IssueCodes.Add("OVER_BUDGET");
            result.Issues.Add(
                $"Total cost {result.TotalCost:F2} LKR exceeds budget {trip.Budget:F2} LKR. " +
                $"Accommodation: {accommodationCost:F2}, Transport: {transportCost:F2}.");
        }

        result.IsValid = result.IssueCodes.Count == 0;
        return result;
    }

    private static void DetectStaleInputs(
        Models.TripProposal proposal,
        Trip trip,
        ValidateProposalResultDto result)
    {
        // Parse the snapshot that was captured when generation started.
        JsonElement snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<JsonElement>(proposal.InputSnapshot);
        }
        catch
        {
            result.Warnings.Add("Could not parse saved input snapshot for stale-input comparison.");
            return;
        }

        var stale = false;

        if (snapshot.TryGetProperty("GroupSize", out var gs) && gs.GetInt32() != trip.GroupSize)
        {
            stale = true;
            result.Issues.Add($"Group size changed from {gs.GetInt32()} (generation) to {trip.GroupSize} (current).");
        }

        if (snapshot.TryGetProperty("Budget", out var bud) && bud.GetDecimal() != trip.Budget)
        {
            stale = true;
            result.Issues.Add($"Budget changed from {bud.GetDecimal()} (generation) to {trip.Budget} (current).");
        }

        if (snapshot.TryGetProperty("StartDate", out var sd))
        {
            var snapshotStart = DateTime.Parse(sd.GetString()!).Date;
            if (snapshotStart != trip.StartDate.Date)
            {
                stale = true;
                result.Issues.Add($"StartDate changed from {snapshotStart:yyyy-MM-dd} (generation) to {trip.StartDate.Date:yyyy-MM-dd} (current).");
            }
        }

        if (snapshot.TryGetProperty("EndDate", out var ed))
        {
            var snapshotEnd = DateTime.Parse(ed.GetString()!).Date;
            if (snapshotEnd != trip.EndDate.Date)
            {
                stale = true;
                result.Issues.Add($"EndDate changed from {snapshotEnd:yyyy-MM-dd} (generation) to {trip.EndDate.Date:yyyy-MM-dd} (current).");
            }
        }

        if (stale)
        {
            result.StaleInputDetected = true;
            result.IssueCodes.Add("STALE_INPUTS");
        }
    }

    private async Task<decimal> ValidateHotelsAsync(
        ValidateProposalRequestDto req,
        Trip trip,
        bool isOneDayTrip,
        ValidateProposalResultDto result)
    {
        if (isOneDayTrip)
        {
            if (req.Hotels.Count > 0)
            {
                result.IssueCodes.Add("ONE_DAY_TRIP_HAS_HOTELS");
                result.Issues.Add("One-day trip must have no hotel stays.");
            }
            return 0m;
        }

        if (req.Hotels.Count == 0)
        {
            result.IssueCodes.Add("MISSING_HOTELS");
            result.Issues.Add("An overnight trip must include hotel stays.");
            return 0m;
        }

        // Load all referenced rooms with their hotels once.
        var roomIds = req.Hotels.Select(h => h.RoomId).Distinct().ToList();
        var rooms = await _db.Rooms
            .Include(r => r.Hotel)
            .Where(r => roomIds.Contains(r.Id))
            .ToListAsync();

        var missingRooms = roomIds.Except(rooms.Select(r => r.Id)).ToList();
        if (missingRooms.Count > 0)
        {
            result.IssueCodes.Add("INVALID_ROOM_IDS");
            result.Issues.Add($"Room IDs not found: {string.Join(", ", missingRooms)}.");
            return 0m;  // Cannot price unknown rooms
        }

        var tripDestinationIds = trip.ItineraryItems.Select(i => i.DestinationId).ToHashSet();

        // Group lines by (HotelId, CheckIn, CheckOut) — each group is one stay.
        var stayGroups = req.Hotels
            .GroupBy(h =>
            {
                var room = rooms.First(r => r.Id == h.RoomId);
                return (room.HotelId, room.Hotel.DestinationId, h.CheckInDate.Date, h.CheckOutDate.Date, room.Hotel.Name);
            })
            .ToList();

        decimal totalAccomCost = 0m;
        var coveredAreaIds = new HashSet<int>();

        foreach (var stay in stayGroups)
        {
            var key = stay.Key;
            var checkIn  = key.Item3;
            var checkOut = key.Item4;
            var destId   = key.DestinationId;
            var hotelName = key.Name;

            // Date ordering
            if (checkOut <= checkIn)
            {
                result.IssueCodes.Add("INVALID_STAY_DATES");
                result.Issues.Add($"Stay at '{hotelName}' has CheckOut {checkOut:yyyy-MM-dd} not after CheckIn {checkIn:yyyy-MM-dd}.");
                continue;
            }

            // Hotel must be in a trip destination
            if (!tripDestinationIds.Contains(destId))
            {
                result.IssueCodes.Add("HOTEL_OUTSIDE_TRIP_AREA");
                result.Issues.Add($"Hotel '{hotelName}' is not in any of the trip's selected areas.");
            }

            // One hotel per area (no repeated stays in same area)
            if (coveredAreaIds.Contains(destId))
            {
                result.IssueCodes.Add("DUPLICATE_AREA_STAY");
                result.Issues.Add($"More than one hotel stay proposed for area {destId} ('{hotelName}'). One hotel per area is required.");
            }
            else
            {
                coveredAreaIds.Add(destId);
            }

            // Per-stay capacity: sum(capacity × quantity) >= GroupSize
            int stayCapacity = 0;
            decimal stayCost = 0m;

            // Aggregate quantities per room to handle duplicate lines correctly.
            var aggregated = stay
                .GroupBy(h => h.RoomId)
                .Select(g => (RoomId: g.Key, TotalQty: g.Sum(h => h.NumberOfRooms)))
                .ToList();

            foreach (var (roomId, qty) in aggregated)
            {
                if (qty <= 0)
                {
                    result.IssueCodes.Add("INVALID_ROOM_QUANTITY");
                    result.Issues.Add($"Room {roomId} in stay at '{hotelName}' has non-positive quantity {qty}.");
                    continue;
                }

                var room = rooms.First(r => r.Id == roomId);
                stayCapacity += room.Capacity * qty;

                int nights = (checkOut - checkIn).Days;
                stayCost += room.PricePerNight * qty * nights;

                // Availability: count active holds + confirmed bookings, aggregate across duplicate lines.
                int alreadyBooked = await _hotelService.CountBookedRoomsAsync(
                    roomId,
                    DateTime.SpecifyKind(checkIn, DateTimeKind.Utc),
                    DateTime.SpecifyKind(checkOut, DateTimeKind.Utc));

                int available = room.TotalRooms - alreadyBooked;
                if (qty > available)
                {
                    result.IssueCodes.Add("ROOM_NOT_AVAILABLE");
                    result.Issues.Add(
                        $"Room {roomId} ({room.Hotel.Name}): requested {qty} but only {available} available " +
                        $"({checkIn:yyyy-MM-dd} – {checkOut:yyyy-MM-dd}).");
                }
            }

            if (stayCapacity < trip.GroupSize)
            {
                result.IssueCodes.Add("CAPACITY_INSUFFICIENT");
                result.Issues.Add(
                    $"Hotel '{hotelName}' provides capacity {stayCapacity} but group size is {trip.GroupSize}.");
            }

            totalAccomCost += stayCost;
        }

        // Date boundary checks: first check-in = trip start, last check-out = trip end.
        var sortedStays = stayGroups
            .Select(g => (CheckIn: g.Key.Item3, CheckOut: g.Key.Item4))
            .OrderBy(s => s.CheckIn)
            .ToList();

        if (sortedStays.Count > 0)
        {
            if (sortedStays.First().CheckIn != trip.StartDate.Date)
            {
                result.IssueCodes.Add("STAY_START_MISMATCH");
                result.Issues.Add(
                    $"First hotel check-in {sortedStays.First().CheckIn:yyyy-MM-dd} must match trip StartDate {trip.StartDate.Date:yyyy-MM-dd}.");
            }

            if (sortedStays.Last().CheckOut != trip.EndDate.Date)
            {
                result.IssueCodes.Add("STAY_END_MISMATCH");
                result.Issues.Add(
                    $"Last hotel check-out {sortedStays.Last().CheckOut:yyyy-MM-dd} must match trip EndDate {trip.EndDate.Date:yyyy-MM-dd}.");
            }

            // Contiguity — no gaps or overlaps between consecutive stays.
            for (int i = 0; i < sortedStays.Count - 1; i++)
            {
                if (sortedStays[i].CheckOut != sortedStays[i + 1].CheckIn)
                {
                    result.IssueCodes.Add("STAY_GAP_OR_OVERLAP");
                    result.Issues.Add(
                        $"Gap or overlap between stay ending {sortedStays[i].CheckOut:yyyy-MM-dd} " +
                        $"and stay starting {sortedStays[i + 1].CheckIn:yyyy-MM-dd}.");
                }
            }
        }

        // Overnight-section agreement: each M1 section must map to a hotel stay covering it.
        if (req.OvernightSections.Count > 0)
        {
            foreach (var section in req.OvernightSections)
            {
                if (!DateTime.TryParse(section.CheckInDate, out var secIn) ||
                    !DateTime.TryParse(section.CheckOutDate, out var secOut))
                {
                    result.Warnings.Add($"Could not parse overnight section dates: {section.CheckInDate}/{section.CheckOutDate}.");
                    continue;
                }

                bool covered = stayGroups.Any(g =>
                    g.Key.Item3 <= secIn.Date && g.Key.Item4 >= secOut.Date);

                if (!covered)
                {
                    result.IssueCodes.Add("SECTION_NOT_COVERED");
                    result.Issues.Add(
                        $"M1 overnight section area {section.OvernightAreaId} " +
                        $"({section.CheckInDate}–{section.CheckOutDate}) has no matching hotel stay.");
                }
            }
        }

        return totalAccomCost;
    }

    private async Task<decimal> ValidateVehicleAsync(
        ValidateProposalRequestDto req,
        Trip trip,
        Models.TripProposal proposal,
        ValidateProposalResultDto result)
    {
        if (req.Vehicle == null)
        {
            // A same-day trip may have no vehicle (valid one-day no-booking is handled upstream).
            // For any trip under the current agentic design M3 always selects a vehicle,
            // so missing vehicle is a NeedsRevision.
            result.IssueCodes.Add("MISSING_VEHICLE");
            result.Issues.Add("No vehicle was proposed. M3 must select exactly one vehicle.");
            return 0m;
        }

        var v = req.Vehicle;

        // Load vehicle from database.
        var vehicle = await _db.Vehicles.FindAsync(v.VehicleId);
        if (vehicle == null)
        {
            result.IssueCodes.Add("INVALID_VEHICLE_ID");
            result.Issues.Add($"Vehicle ID {v.VehicleId} does not exist.");
            return 0m;
        }

        if (vehicle.Status != VehicleStatus.Active)
        {
            result.IssueCodes.Add("VEHICLE_NOT_ACTIVE");
            result.Issues.Add($"Vehicle {v.VehicleId} is not active (status: {vehicle.Status}).");
        }

        // Passenger capacity.
        if (vehicle.Capacity < trip.GroupSize)
        {
            result.IssueCodes.Add("VEHICLE_CAPACITY_INSUFFICIENT");
            result.Issues.Add(
                $"Vehicle {v.VehicleId} capacity {vehicle.Capacity} is less than group size {trip.GroupSize}.");
        }

        // Rental must cover the whole trip.
        if (v.StartDate.Date != trip.StartDate.Date || v.EndDate.Date != trip.EndDate.Date)
        {
            result.IssueCodes.Add("VEHICLE_DATE_MISMATCH");
            result.Issues.Add(
                $"Vehicle rental {v.StartDate.Date:yyyy-MM-dd}–{v.EndDate.Date:yyyy-MM-dd} " +
                $"must match trip dates {trip.StartDate.Date:yyyy-MM-dd}–{trip.EndDate.Date:yyyy-MM-dd}.");
        }

        // Pickup coordinates must be present and within valid ranges.
        if (v.PickupLatitude < -90m || v.PickupLatitude > 90m ||
            v.PickupLongitude < -180m || v.PickupLongitude > 180m)
        {
            result.IssueCodes.Add("INVALID_PICKUP_COORDINATES");
            result.Issues.Add(
                $"Pickup coordinates ({v.PickupLatitude}, {v.PickupLongitude}) are out of valid range.");
        }

        // Pickup must match the snapshot — use the already-loaded proposal.InputSnapshot.
        try
        {
            var snap = JsonSerializer.Deserialize<JsonElement>(proposal.InputSnapshot);

            if (snap.TryGetProperty("PickupLatitude", out var snapLat) &&
                snap.TryGetProperty("PickupLongitude", out var snapLon))
            {
                if (Math.Abs(snapLat.GetDecimal() - v.PickupLatitude) > 0.0001m ||
                    Math.Abs(snapLon.GetDecimal() - v.PickupLongitude) > 0.0001m)
                {
                    result.IssueCodes.Add("PICKUP_MISMATCH");
                    result.Issues.Add(
                        "Vehicle pickup coordinates do not match the traveler's recorded pickup location.");
                }
            }
        }
        catch
        {
            result.Warnings.Add("Could not verify pickup coordinates against saved snapshot.");
        }

        // Availability check.
        bool available = await _vehicleService.IsVehicleAvailableAsync(
            v.VehicleId,
            DateTime.SpecifyKind(v.StartDate, DateTimeKind.Utc),
            DateTime.SpecifyKind(v.EndDate, DateTimeKind.Utc));

        if (!available)
        {
            result.IssueCodes.Add("VEHICLE_NOT_AVAILABLE");
            result.Issues.Add(
                $"Vehicle {v.VehicleId} is not available for {v.StartDate.Date:yyyy-MM-dd}–{v.EndDate.Date:yyyy-MM-dd}.");
        }

        // Price using confirmed vehicle billing rule.
        // Same-day (start == end) = 1 rental day; multi-day = date difference.
        int days = Math.Max(1, (v.EndDate.Date - v.StartDate.Date).Days);
        decimal cost = vehicle.PricePerDay * days;

        return cost;
    }
}
