using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/supplies")]
public class SuppliesController : ControllerBase
{
    private readonly ISupplyService _supplyService;

    public SuppliesController(ISupplyService supplyService)
    {
        _supplyService = supplyService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found in token.");
        return int.Parse(claim);
    }

    // ── Supplier endpoints ───────────────────────────────────────────────────

    /// <summary>Create a new supply item. Starts as Active immediately (no listing approval step). Supplier only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplyDetailDto>>> Create([FromBody] CreateSupplyDto dto)
    {
        var result = await _supplyService.CreateAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetMySupplyById), new { id = result.Id },
            ApiResponse<SupplyDetailDto>.Ok(result, "Supply item created successfully."));
    }

    /// <summary>Get the supplier's own supplies with optional search, category, status, sort, and pagination. Supplier only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplySummaryDto>>>> GetMySupplies(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _supplyService.GetMySuppliesAsync(
            GetCurrentUserId(), search, category, status, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<SupplySummaryDto>>.Ok(result));
    }

    /// <summary>Get full detail of one of the supplier's own supplies. Supplier only.</summary>
    [HttpGet("my/{id}")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplyDetailDto>>> GetMySupplyById(int id)
    {
        var result = await _supplyService.GetMySupplyByIdAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<SupplyDetailDto>.Ok(result));
    }

    /// <summary>Update an existing supply item. Supplier owner only.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplyDetailDto>>> Update(int id, [FromBody] UpdateSupplyDto dto)
    {
        var result = await _supplyService.UpdateAsync(id, dto, GetCurrentUserId());
        return Ok(ApiResponse<SupplyDetailDto>.Ok(result, "Supply item updated successfully."));
    }

    /// <summary>Deactivate a supply item (sets Status = Inactive). Supplier owner only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse>> Deactivate(int id)
    {
        await _supplyService.DeactivateAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok("Supply item deactivated."));
    }

    /// <summary>Republish an Inactive or Removed supply item back to Active status. Supplier owner only.</summary>
    [HttpPost("{id}/republish")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<SupplyDetailDto>>> Republish(int id)
    {
        var result = await _supplyService.RepublishAsync(id, GetCurrentUserId());
        return Ok(ApiResponse<SupplyDetailDto>.Ok(result, "Supply item republished as Active."));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List ALL supplies across all suppliers with filters, search, and pagination. Admin/SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplySummaryDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] int? supplierId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _supplyService.GetAllAdminAsync(
            search, category, status, supplierId, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<SupplySummaryDto>>.Ok(result));
    }

    /// <summary>Remove a supply item from catalog citing a reason and optional note. Admin/SuperAdmin only.</summary>
    [HttpPost("{id}/remove")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<SupplyDetailDto>>> Remove(int id, [FromBody] RemoveSupplyDto dto)
    {
        var result = await _supplyService.RemoveAsync(id, dto);
        return Ok(ApiResponse<SupplyDetailDto>.Ok(result, "Supply item removed by admin."));
    }

    // ── Public / Authenticated Browse endpoints ───────────────────────────────

    /// <summary>Browse available active supplies with optional search, category filter, sort, and pagination. Any authenticated user.</summary>
    [HttpGet("browse")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplySummaryDto>>>> Browse(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _supplyService.BrowseActiveAsync(search, category, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<SupplySummaryDto>>.Ok(result));
    }
}
