namespace TourManagement.Api.Models;

// Represents a registered user in the system.
// Roles: Traveler, HotelOwner, TransportProvider, Supplier, Admin, SuperAdmin.
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // BCrypt hash of the password. We never store or log the raw password.
    public string PasswordHash { get; set; } = string.Empty;

    // Role is stored as a plain string so it matches the Roles.cs constants
    // and works directly with [Authorize(Roles = ...)].
    public string Role { get; set; } = string.Empty;

    // Stored as a string in the DB (see AppDbContext). Default is Active.
    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — EF Core uses these to join related tables.
    public List<Trip> Trips { get; set; } = new();

    // Hotels this user owns (only populated when Role = HotelOwner).
    public List<Hotel> Hotels { get; set; } = new();
}
