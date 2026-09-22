using TourManagement.Api.Dtos.Profile;

namespace TourManagement.Api.Profile;

public interface IAccountDeletionGuard
{
    Task<DeletionEligibilityDto> CheckAsync(int userId);
}
