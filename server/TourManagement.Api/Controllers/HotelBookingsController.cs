using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Accommodation;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

// Public endpoints for hotel booking management.
[ApiController]
[Route("api/hotel-bookings")]
public class HotelBookingsController : ControllerBase
{
    private readonly IHotelBookingService _bookingService;

    public HotelBookingsController(IHotelBookingService bookingService)
    {
        _bookingService = bookingService;
    }

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

    /// <summary>Create a hotel booking for a trip. Starts as Held. Traveler only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<HotelBookingSummaryDto>>> Create([FromBody] CreateHotelBookingDto dto)
    {
        var result = await _bookingService.CreateAsync(dto, GetCurrentUserId());
        return Ok(ApiResponse<HotelBookingSummaryDto>.Ok(result, "Hotel booking created (Held)."));
    }

    /// <summary>Get the logged-in traveler's own hotel bookings. Traveler only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<PagedResult<HotelBookingSummaryDto>>>> GetMyBookings(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _bookingService.GetMyBookingsAsync(GetCurrentUserId(), status, page, pageSize);
        return Ok(ApiResponse<PagedResult<HotelBookingSummaryDto>>.Ok(result));
    }

    /// <summary>Cancel a hotel booking. Trip owner or Admin/SuperAdmin only.</summary>
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Cancel(int id)
    {
        await _bookingService.CancelAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse.Ok("Hotel booking cancelled."));
    }
}
