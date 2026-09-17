using System.ComponentModel.DataAnnotations;

namespace TourManagement.Api.Dtos.Auth;

// What the client sends to log in.
public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
