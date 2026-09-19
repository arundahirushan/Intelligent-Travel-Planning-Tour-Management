namespace TourManagement.Api.Models;

// A traveler's booking of a specific vehicle for a specific trip.
// Named VehicleBooking (not Booking) to avoid colliding with the hotel Booking entity.
public class VehicleBooking
{
    public int Id { get; set; }

    // The trip this vehicle booking belongs to.
    public int TripId { get; set; }

    // The vehicle being booked. Each vehicle is one physical unit —
    // it's either available for the whole window or it isn't.
    public int VehicleId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Where the traveler wants the vehicle delivered.
    // These come from a map picker on the frontend and are stored as-is.
    public decimal PickupLatitude { get; set; }
    public decimal PickupLongitude { get; set; }

    // Optional extra instructions, e.g. "gate code 1234". Nullable.
    public string? PickupNote { get; set; }

    // Held = tentative (blocks availability), Confirmed = finalized, Cancelled = no-op.
    // Stored as a string in the DB (see AppDbContext). Uses the shared BookingStatus enum.
    public BookingStatus Status { get; set; } = BookingStatus.Held;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties.
    public Trip Trip { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
}

