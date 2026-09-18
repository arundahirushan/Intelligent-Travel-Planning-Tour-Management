using TourManagement.Api.Models;

namespace TourManagement.Api.Dtos.Auth;

// What the API returns after a successful registration.
public class RegisterResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public UserStatus Status { get; set; }

    // Human-readable message explaining what happens next.
    // e.g. "Registration successful." or "Registration successful, awaiting admin approval."
    public string Message { get; set; } = string.Empty;
}
