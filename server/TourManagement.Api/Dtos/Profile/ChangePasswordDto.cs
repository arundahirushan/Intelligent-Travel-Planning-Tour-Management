using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Profile;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
