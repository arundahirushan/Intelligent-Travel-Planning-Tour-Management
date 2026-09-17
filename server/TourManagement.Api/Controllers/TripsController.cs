using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Trips;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    // Helper: reads the user's ID from the JWT claims.
    // ClaimTypes.NameIdentifier is the claim we set in AuthService.GenerateJwt.
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found in token.");
        return int.Parse(claim);
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Role claim not found in token.");
    }

    // ── Traveler endpoints ───────────────────────────────────────────────────

    /// <summary>Create a new trip (starts as Draft). Traveler only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> Create([FromBody] CreateTripDto dto)
    {
        var result = await _tripService.CreateAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<TripDetailDto>.Ok(result, "Trip created."));
    }

    /// <summary>Get the logged-in traveler's own trips. Traveler only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<PagedResult<TripSummaryDto>>>> GetMyTrips(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _tripService.GetMyTripsAsync(
            GetCurrentUserId(), status, search, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<TripSummaryDto>>.Ok(result));
    }

    /// <summary>Get full trip details. Owner or Admin/SuperAdmin only (checked in service).</summary>
    [HttpGet("{id}")]
    [Authorize]  // any authenticated user — ownership check happens inside the service
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> GetById(int id)
    {
        var result = await _tripService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse<TripDetailDto>.Ok(result));
    }

    /// <summary>Update a trip (only while Draft). Traveler only.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> Update(int id, [FromBody] UpdateTripDto dto)
    {
        var result = await _tripService.UpdateAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<TripDetailDto>.Ok(result, "Trip updated."));
    }

    /// <summary>Cancel a trip (only if Draft or Planned). Sets Status = Cancelled. Traveler only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse>> Cancel(int id)
    {
        await _tripService.CancelAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok("Trip cancelled."));
    }

    /// <summary>Add an itinerary item to a trip. Traveler (owner) only.</summary>
    [HttpPost("{id}/itinerary-items")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> AddItineraryItem(
        int id, [FromBody] CreateItineraryItemDto dto)
    {
        var result = await _tripService.AddItineraryItemAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<TripDetailDto>.Ok(result, "Itinerary item added."));
    }

    /// <summary>Update an itinerary item. Traveler (owner) only.</summary>
    [HttpPut("{id}/itinerary-items/{itemId}")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> UpdateItineraryItem(
        int id, int itemId, [FromBody] UpdateItineraryItemDto dto)
    {
        var result = await _tripService.UpdateItineraryItemAsync(id, itemId, dto, GetCurrentUserId());
        return Ok(ApiResponse<TripDetailDto>.Ok(result, "Itinerary item updated."));
    }

    /// <summary>Remove an itinerary item. Traveler (owner) only.</summary>
    [HttpDelete("{id}/itinerary-items/{itemId}")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> RemoveItineraryItem(int id, int itemId)
    {
        var result = await _tripService.RemoveItineraryItemAsync(id, itemId, GetCurrentUserId());
        return Ok(ApiResponse<TripDetailDto>.Ok(result, "Itinerary item removed."));
    }

    /// <summary>Auto-generate a draft itinerary by splitting trip days across destinations. Traveler only.</summary>
    [HttpPost("{id}/generate-draft-itinerary")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<TripDetailDto>>> GenerateDraftItinerary(
        int id, [FromBody] GenerateDraftItineraryDto dto)
    {
        var result = await _tripService.GenerateDraftItineraryAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<TripDetailDto>.Ok(result, "Draft itinerary generated."));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List ALL trips across all travelers. Admin / SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<TripSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? destinationId,
        [FromQuery] int? travelerId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _tripService.GetAllAsync(
            status, destinationId, travelerId, search, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<TripSummaryDto>>.Ok(result));
    }

    /// <summary>Force-cancel any trip regardless of its current status. Admin / SuperAdmin only.</summary>
    [HttpPost("{id}/force-cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> ForceCancel(int id)
    {
        await _tripService.ForceCancelAsync(id);
        return Ok(ApiResponse.Ok("Trip force-cancelled."));
    }
}
