using TourManagement.Api.Dtos.Profile;
using TourManagement.Api.Dtos.User;

namespace TourManagement.Api.Services.Interfaces;

public interface IProfileService
{
    Task<UserSummaryDto> GetMyProfileAsync(int userId);
    Task<UserSummaryDto> UpdateMyProfileAsync(int userId, UpdateProfileDto dto);
    Task<DeletionEligibilityDto> CheckDeletionEligibilityAsync(int userId, string role);
    Task DeleteMyAccountAsync(int userId, string role);
}
