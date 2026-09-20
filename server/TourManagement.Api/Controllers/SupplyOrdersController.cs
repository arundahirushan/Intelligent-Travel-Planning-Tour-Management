using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.SupplyOrders;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/supply-orders")]
public class SupplyOrdersController : ControllerBase
{
    private readonly ISupplyOrderService _supplyOrderService;

    public SupplyOrdersController(ISupplyOrderService supplyOrderService)
    {
        _supplyOrderService = supplyOrderService;
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

    /// <summary>Create a new supply order for a trip. Traveler only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<SupplyOrderSummaryDto>>> Create([FromBody] CreateSupplyOrderDto dto)
    {
        var result = await _supplyOrderService.CreateAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetMyOrders), new { id = result.Id },
            ApiResponse<SupplyOrderSummaryDto>.Ok(result, "Supply order placed successfully."));
    }

    /// <summary>Get the traveler's own supply orders. Traveler only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplyOrderSummaryDto>>>> GetMyOrders(
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _supplyOrderService.GetMyOrdersAsync(
            GetCurrentUserId(), status, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<SupplyOrderSummaryDto>>.Ok(result));
    }

    /// <summary>Cancel a supply order. Traveler or Admin.</summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = $"{Roles.Traveler},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<SupplyOrderSummaryDto>>> Cancel(int id)
    {
        var result = await _supplyOrderService.CancelAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<SupplyOrderSummaryDto>.Ok(result, "Supply order cancelled."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Traveler)]
    public async Task<ActionResult<ApiResponse<SupplyOrderSummaryDto>>> Update(int id, [FromBody] UpdateSupplyOrderDto dto)
    {
        var result = await _supplyOrderService.UpdateAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<SupplyOrderSummaryDto>.Ok(result, "Supply order updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Traveler},{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _supplyOrderService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse.Ok("Supply order deleted."));
    }

    // ── Supplier endpoints ───────────────────────────────────────────────────

    /// <summary>Get orders placed against this supplier's own supplies. Supplier only.</summary>
    [HttpGet("received")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplyOrderSummaryDto>>>> GetReceivedOrders(
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _supplyOrderService.GetReceivedOrdersAsync(
            GetCurrentUserId(), status, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<SupplyOrderSummaryDto>>.Ok(result));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List ALL supply orders across all suppliers/travelers. Admin/SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplyOrderSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _supplyOrderService.GetAllAdminAsync(status, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<SupplyOrderSummaryDto>>.Ok(result));
    }
}
