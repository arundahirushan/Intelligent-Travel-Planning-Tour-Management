namespace TourManagement.Api.Models;

// Represents a vehicle offered by a TransportProvider as a rental-with-driver service.
// The transport company always provides a driver — there is no driver management in this system.
public class Vehicle
{
    public int Id { get; set; }

    // The TransportProvider user who owns and manages this vehicle listing.
    public int ProviderId { get; set; }

    // Free-text category, e.g. "Car", "Van", "Bus". No enum — providers describe it as they see fit.
    public string VehicleType { get; set; } = string.Empty;

    // Make and model, e.g. "Toyota KDH".
    public string Model { get; set; } = string.Empty;

    // Sri Lankan vehicle registration number. Must be unique across all vehicles.
    public string RegistrationNumber { get; set; } = string.Empty;

    // Maximum number of passengers this vehicle can carry.
    public int Capacity { get; set; }

    // Daily rental price in LKR (this project is Sri Lanka only).
    public decimal PricePerDay { get; set; }

    // Stored as a string in the DB (see AppDbContext).
    public VehicleStatus Status { get; set; } = VehicleStatus.PendingApproval;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — EF Core uses these to join related tables.
    public User Provider { get; set; } = null!;
    public List<VehicleBooking> VehicleBookings { get; set; } = new();
}

