using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Auth;

// What the client sends when creating a new account.
public class RegisterRequestDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    // Must be one of the values in Roles.cs (validated in the service layer).
    [Required]
    public string Role { get; set; } = string.Empty;
}
