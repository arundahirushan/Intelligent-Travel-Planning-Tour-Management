using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/vehicle-bookings")]
public class VehicleBookingsController : ControllerBase
{
    private readonly IVehicleBookingService _bookingService;

    public VehicleBookingsController(IVehicleBookingService bookingService)
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

    /// <summary>Create a vehicle booking for a trip. Starts as Held. Traveler only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<VehicleBookingSummaryDto>>> Create([FromBody] CreateVehicleBookingDto dto)
    {
        var result = await _bookingService.CreateAsync(dto, GetCurrentUserId());
        return Ok(ApiResponse<VehicleBookingSummaryDto>.Ok(result, "Vehicle booking created (Held)."));
    }

    /// <summary>Get the logged-in traveler's own vehicle bookings. Traveler only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleBookingSummaryDto>>>> GetMyBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _bookingService.GetMyBookingsAsync(GetCurrentUserId(), page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleBookingSummaryDto>>.Ok(result));
    }

    /// <summary>Cancel a vehicle booking. Trip owner or Admin/SuperAdmin only.</summary>
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Cancel(int id)
    {
        await _bookingService.CancelAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse.Ok("Vehicle booking cancelled."));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>All vehicle bookings, oversight view. Admin / SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleBookingSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _bookingService.GetAllBookingsAsync(status, page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleBookingSummaryDto>>.Ok(result));
    }
}

