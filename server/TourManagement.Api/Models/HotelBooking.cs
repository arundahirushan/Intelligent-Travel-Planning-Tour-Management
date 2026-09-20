namespace TourManagement.Api.Models;

// A traveler's booking of a specific room type for a specific trip.
// Renamed from "Booking" to "HotelBooking" to sit consistently alongside VehicleBooking.
// NumberOfRooms lets a traveler book multiple rooms of the same type at once.
public class HotelBooking
{
    public int Id { get; set; }

    // The trip this accommodation booking belongs to.
    public int TripId { get; set; }

    // The room type being booked.
    public int RoomId { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    // How many rooms of this type the traveler is booking (e.g. 2 rooms for a family).
    public int NumberOfRooms { get; set; }

    // Held = tentative (counts against availability), Confirmed = finalized, Cancelled = no-op.
    // Stored as a string in the DB (see AppDbContext).
    public BookingStatus Status { get; set; } = BookingStatus.Held;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties.
    public Trip Trip { get; set; } = null!;
    public Room Room { get; set; } = null!;
}
