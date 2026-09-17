using TourManagement.Api.Common;
using TourManagement.Api.Dtos.User;

namespace TourManagement.Api.Services.Interfaces;

public interface IUserService
{
    // Admin + SuperAdmin: list users awaiting approval, optionally filtered by role.
    Task<PagedResult<UserSummaryDto>> GetPendingAsync(string? role, int page, int pageSize);

    // Admin + SuperAdmin: approve a pending user.
    Task<UserSummaryDto> ApproveAsync(int id);

    // Admin + SuperAdmin: reject a pending user.
    Task<UserSummaryDto> RejectAsync(int id);

    // Admin + SuperAdmin: list all users with optional search, filter, sort, paging.
    Task<PagedResult<UserSummaryDto>> GetAllAsync(
        string? search, string? role, string? status, string? sort, int page, int pageSize);

    // SuperAdmin only: create a new Admin account directly.
    Task<UserSummaryDto> CreateAdminAsync(CreateAdminRequestDto dto);

    // SuperAdmin only: promote an Admin to SuperAdmin.
    Task<UserSummaryDto> PromoteToSuperAdminAsync(int id);

    // SuperAdmin only: delete any user account.
    Task DeleteUserAsync(int id);
}
