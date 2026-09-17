using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.User;

// Used by SuperAdmin to create a new Admin account directly.
// Role is intentionally NOT in this DTO — it is always fixed to Admin
// inside the service. We never trust the client to set it.
public class CreateAdminRequestDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}
