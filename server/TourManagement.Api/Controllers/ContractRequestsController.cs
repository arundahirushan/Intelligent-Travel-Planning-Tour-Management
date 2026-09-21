using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.Supplier;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/contract-requests")]
public class ContractRequestsController : ControllerBase
{
    private readonly IContractRequestService _contractRequestService;

    public ContractRequestsController(IContractRequestService contractRequestService)
    {
        _contractRequestService = contractRequestService;
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

    // ── Supplier endpoints ───────────────────────────────────────────────────

    /// <summary>Submit a new contract or renewal request. Supplier only.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<ContractRequestSummaryDto>>> CreateRequest(
        [FromBody] CreateContractRequestDto dto)
    {
        var result = await _contractRequestService.CreateRequestAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ContractRequestSummaryDto>.Ok(result, "Contract request submitted successfully."));
    }

    /// <summary>Get the logged-in supplier's submitted contract requests. Supplier only.</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Supplier)]
    public async Task<ActionResult<ApiResponse<PagedResult<ContractRequestSummaryDto>>>> GetMyRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _contractRequestService.GetMyRequestsAsync(GetCurrentUserId(), page, pageSize);
        return Ok(ApiResponse<PagedResult<ContractRequestSummaryDto>>.Ok(result));
    }

    /// <summary>Get contract request details by ID. Owner or Admin/SuperAdmin.</summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ContractRequestSummaryDto>>> GetById(int id)
    {
        var result = await _contractRequestService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentUserRole());
        return Ok(ApiResponse<ContractRequestSummaryDto>.Ok(result));
    }

    // ── Admin / SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List all contract requests with optional status filter and pagination. Admin/SuperAdmin only.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<ContractRequestSummaryDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _contractRequestService.GetAllAdminAsync(status, page, pageSize);
        return Ok(ApiResponse<PagedResult<ContractRequestSummaryDto>>.Ok(result));
    }

    /// <summary>Approve a pending contract request. Admin/SuperAdmin only.</summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<ContractRequestSummaryDto>>> Approve(int id)
    {
        var result = await _contractRequestService.ApproveAsync(id);
        return Ok(ApiResponse<ContractRequestSummaryDto>.Ok(result, "Contract request approved."));
    }

    /// <summary>Reject a pending contract request with an optional note. Admin/SuperAdmin only.</summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<ContractRequestSummaryDto>>> Reject(
        int id, [FromBody] RejectContractRequestDto dto)
    {
        var result = await _contractRequestService.RejectAsync(id, dto);
        return Ok(ApiResponse<ContractRequestSummaryDto>.Ok(result, "Contract request rejected."));
    }
}
