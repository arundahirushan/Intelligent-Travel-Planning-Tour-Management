using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourManagement.Api.Common;
using TourManagement.Api.Dtos.Profile;
using TourManagement.Api.Dtos.User;
using TourManagement.Api.Services.Interfaces;

namespace TourManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Any authenticated user can access their own profile
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var profile = await _profileService.GetMyProfileAsync(userId);
        return Ok(ApiResponse<UserSummaryDto>.Ok(profile));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> UpdateMyProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var profile = await _profileService.UpdateMyProfileAsync(userId, dto);
        return Ok(ApiResponse<UserSummaryDto>.Ok(profile, "Profile updated successfully."));
    }

    [HttpGet("me/deletion-eligibility")]
    public async Task<ActionResult<ApiResponse<DeletionEligibilityDto>>> CheckDeletionEligibility()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        
        var eligibility = await _profileService.CheckDeletionEligibilityAsync(userId, role);
        return Ok(ApiResponse<DeletionEligibilityDto>.Ok(eligibility));
    }

    [HttpDelete("me")]
    public async Task<ActionResult<ApiResponse>> DeleteMyAccount()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        
        await _profileService.DeleteMyAccountAsync(userId, role);
        return Ok(ApiResponse.Ok("Your account has been successfully deleted."));
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(idClaim!);
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
