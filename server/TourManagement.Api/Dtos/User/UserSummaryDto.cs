using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.User;

// A lightweight user summary used in list views (pending approvals, all users).
public class UserSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
