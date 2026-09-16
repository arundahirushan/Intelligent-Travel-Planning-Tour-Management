using TourManagement.Api.Dtos.User;
using TourManagement.Api.Models;

namespace TourManagement.Api.Mappings;

// Extension methods to convert User entities to DTOs.
// Static methods keep the mapping logic visible and easy to find —
// no hidden configuration in a separate mapper registration.
public static class UserMappings
{
    public static UserSummaryDto ToSummaryDto(this User user)
    {
        return new UserSummaryDto
        {
            Id        = user.Id,
            FullName  = user.FullName,
            Email     = user.Email,
            Role      = user.Role,
            Status    = user.Status,
            CreatedAt = user.CreatedAt
        };
    }
}
