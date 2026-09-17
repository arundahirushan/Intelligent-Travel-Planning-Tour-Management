using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagement.Api.Common;
using TourManagement.Api.Common.Constants;
using TourManagement.Api.Dtos.User;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // ── Admin + SuperAdmin endpoints ─────────────────────────────────────────

    /// <summary>List users waiting for approval, optionally filtered by role.</summary>
    [HttpGet("pending")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserSummaryDto>>>> GetPending(
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetPendingAsync(role, page, pageSize);
        return Ok(ApiResponse<PagedResult<UserSummaryDto>>.Ok(result));
    }

    /// <summary>Approve a pending user account.</summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> Approve(int id)
    {
        var result = await _userService.ApproveAsync(id);
        return Ok(ApiResponse<UserSummaryDto>.Ok(result, "User approved successfully."));
    }

    /// <summary>Reject a pending user account.</summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> Reject(int id)
    {
        var result = await _userService.RejectAsync(id);
        return Ok(ApiResponse<UserSummaryDto>.Ok(result, "User rejected."));
    }

    /// <summary>List all users with search, filter, sort, and pagination.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.SuperAdmin}")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserSummaryDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetAllAsync(search, role, status, sort, page, pageSize);
        return Ok(ApiResponse<PagedResult<UserSummaryDto>>.Ok(result));
    }

    // ── SuperAdmin-only endpoints ────────────────────────────────────────────

    /// <summary>Create a new Admin account (SuperAdmin only).</summary>
    [HttpPost("admins")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> CreateAdmin([FromBody] CreateAdminRequestDto dto)
    {
        var result = await _userService.CreateAdminAsync(dto);
        return CreatedAtAction(nameof(GetAll), ApiResponse<UserSummaryDto>.Ok(result, "Admin account created."));
    }

    /// <summary>Promote an Admin to SuperAdmin.</summary>
    [HttpPost("{id}/promote")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> Promote(int id)
    {
        var result = await _userService.PromoteToSuperAdminAsync(id);
        return Ok(ApiResponse<UserSummaryDto>.Ok(result, "User promoted to SuperAdmin."));
    }

    /// <summary>Permanently delete a user account.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        await _userService.DeleteUserAsync(id);
        return Ok(ApiResponse.Ok("User deleted."));
    }
}
