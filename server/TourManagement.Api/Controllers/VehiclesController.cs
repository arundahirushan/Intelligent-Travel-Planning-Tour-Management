using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Transport;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
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

    // ── TransportProvider endpoints ───────────────────────────────────────────

    /// <summary>Create a new vehicle listing. Starts as PendingApproval. TransportProvider only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse<VehicleDetailDto>>> Create([FromBody] CreateVehicleDto dto)
    {
        var result = await _vehicleService.CreateVehicleAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<VehicleDetailDto>.Ok(result, "Vehicle created. Pending Admin approval."));
    }

    /// <summary>Get the provider's own vehicles with optional search, filter, sort, and pagination. TransportProvider only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleSummaryDto>>>> GetMyVehicles(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _vehicleService.GetMyVehiclesAsync(GetCurrentUserId(), search, status, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleSummaryDto>>.Ok(result));
    }

    /// <summary>Get full detail of one of the provider's own vehicles. TransportProvider only.</summary>
    [HttpGet("my/{id}")]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse<VehicleDetailDto>>> GetMyVehicleById(int id)
    {
        var result = await _vehicleService.GetMyVehicleByIdAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<VehicleDetailDto>.Ok(result));
    }

    /// <summary>Update vehicle details. TransportProvider only (ownership checked in service).</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse<VehicleDetailDto>>> Update(int id, [FromBody] UpdateVehicleDto dto)
    {
        var result = await _vehicleService.UpdateVehicleAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<VehicleDetailDto>.Ok(result, "Vehicle updated."));
    }

    /// <summary>Soft-delete a vehicle (sets Status = Inactive). TransportProvider only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _vehicleService.DeactivateVehicleAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok("Vehicle deactivated."));
    }

    /// <summary>Get bookings for this vehicle. TransportProvider only (ownership checked in service).</summary>
    [HttpGet("{vehicleId}/bookings")]
    [Authorize(Roles = Roles.TransportProvider)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleBookingSummaryDto>>>> GetVehicleBookings(
        int vehicleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _vehicleService.GetVehicleBookingsAsync(vehicleId, GetCurrentUserId(), page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleBookingSummaryDto>>.Ok(result));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List ALL vehicles with filters. Admin / SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? providerId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _vehicleService.GetAllVehiclesAsync(status, providerId, search, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleSummaryDto>>.Ok(result));
    }

    /// <summary>List vehicles awaiting approval. Admin / SuperAdmin only.</summary>
    [HttpGet("pending")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleSummaryDto>>>> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _vehicleService.GetPendingVehiclesAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<VehicleSummaryDto>>.Ok(result));
    }

    /// <summary>Approve a vehicle (Status → Active). Admin / SuperAdmin only.</summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Approve(int id)
    {
        await _vehicleService.ApproveVehicleAsync(id);
        return Ok(ApiResponse.Ok("Vehicle approved."));
    }

    /// <summary>
    /// Reject a pending vehicle listing (Status → Rejected). Admin / SuperAdmin only.
    /// Only valid when the vehicle is still PendingApproval.
    /// To disable an already-Active vehicle, use the suspend endpoint instead.
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Reject(int id)
    {
        await _vehicleService.RejectVehicleAsync(id);
        return Ok(ApiResponse.Ok("Vehicle listing rejected."));
    }

    /// <summary>Suspend an active vehicle (Status → Suspended). Admin / SuperAdmin only. Use reject for pending listings.</summary>
    [HttpPost("{id}/suspend")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Suspend(int id)
    {
        await _vehicleService.SuspendVehicleAsync(id);
        return Ok(ApiResponse.Ok("Vehicle suspended."));
    }

    // ── Public / Traveler endpoints ──────────────────────────────────────────

    /// <summary>Search available vehicles by dates, capacity, and price. Any authenticated user.</summary>
    [HttpGet("search")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<VehicleSearchResultDto>>>> Search(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? minCapacity,
        [FromQuery] decimal? maxPricePerDay)
    {
        var request = new VehicleSearchRequestDto
        {
            StartDate      = startDate,
            EndDate        = endDate,
            MinCapacity    = minCapacity,
            MaxPricePerDay = maxPricePerDay
        };

        var result = await _vehicleService.SearchAsync(request);
        return Ok(ApiResponse<List<VehicleSearchResultDto>>.Ok(result));
    }

    /// <summary>Get vehicle detail. Active vehicles visible to all; provider and admins see any status.</summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<VehicleDetailDto>>> GetById(int id)
    {
        var result = await _vehicleService.GetPublicVehicleByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse<VehicleDetailDto>.Ok(result));
    }
}

