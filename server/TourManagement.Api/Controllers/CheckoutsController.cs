using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Checkout;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/checkouts")]
public class CheckoutsController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;

    public CheckoutsController(ICheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
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

    /// <summary>
    /// Place a 12-hour hold on a hotel room, a vehicle, or both.
    /// Provide Hotel, Vehicle, or both in the request body.
    /// The backend verifies ownership, availability, and calculates prices itself.
    /// Returns the same checkout if an identical active hold already exists (safe to retry).
    /// Traveler only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<CheckoutResponseDto>>> PlaceHold(
        [FromBody] CreateCheckoutDto dto)
    {
        var result = await _checkoutService.PlaceHoldAsync(dto, GetCurrentUserId());
        return Ok(ApiResponse<CheckoutResponseDto>.Ok(result, "Hold placed successfully. You have 12 hours to complete payment."));
    }

    /// <summary>
    /// Get all checkouts (hold sessions) for the logged-in traveler. Traveler only.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<PagedResult<CheckoutResponseDto>>>> GetMyCheckouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _checkoutService.GetMyCheckoutsAsync(GetCurrentUserId(), page, pageSize);
        return Ok(ApiResponse<PagedResult<CheckoutResponseDto>>.Ok(result));
    }

    /// <summary>
    /// Get a single checkout (hold session) by ID. Traveler only.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<CheckoutResponseDto>>> GetById(int id)
    {
        var result = await _checkoutService.GetByIdAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<CheckoutResponseDto>.Ok(result));
    }

    /// <summary>
    /// Cancel an active checkout. Sets the checkout and linked bookings to Cancelled.
    /// Traveler or Admin/SuperAdmin only.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Cancel(int id)
    {
        // Admin can cancel any checkout; traveler can only cancel their own (enforced in service).
        await _checkoutService.CancelAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok("Checkout cancelled."));
    }

    // ── PayHere integration point ─────────────────────────────────────────────
    // POST /{id}/confirm is reserved for the future PayHere callback handler.
    // It is NOT exposed here — the PayHere callback will be a separate unauthenticated
    // endpoint verified by HMAC signature.  Do not add a confirm endpoint in this task.
    // ──────────────────────────────────────────────────────────────────────────
}
